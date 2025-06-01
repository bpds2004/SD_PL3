using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Grpc.Net.Client;
using Preprocess; 
using System.Data.SQLite;


class Agregador
{
    static void Main()
    {
        BaseDados.Inicializar();
        Console.WriteLine("[AGREGADOR] Iniciando o Agregador...");
        Console.Write("[AGREGADOR] Quantos Agregadores pretende criar? ");
        int numAgregadores = int.Parse(Console.ReadLine());

        int[] portas = new int[numAgregadores];
        for (int i = 0; i < numAgregadores; i++)
        {
            Console.Write($"[AGREGADOR] Porta para o Agregador #{i + 1}: ");
            portas[i] = int.Parse(Console.ReadLine());
        }

        foreach (int porta in portas)
        {
            new Thread(() => IniciarAgregador(porta)).Start();
        }

        Console.WriteLine("\n[AGREGADOR] Todos os Agregadores estão à escuta! Pressiona Enter para sair...");
        Console.ReadLine();
    }

    static void IniciarAgregador(int porta)
    {
        // Subscrição a RabbitMQ
        new Thread(() => SubscreverRabbitMQ(porta)).Start();

        // TCP server para manter compatibilidade (opcional)
        TcpListener listener = new TcpListener(IPAddress.Any, porta);
        listener.Start();
        Console.WriteLine($"[AGREGADOR:{porta}] À espera de WAVYs na porta {porta} (TCP antigo)...");

        while (true)
        {
            TcpClient wavy = listener.AcceptTcpClient();
            new Thread(() => TratarWavy(wavy, porta)).Start();
        }
    }

    static void SubscreverRabbitMQ(int porta)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: "sensores", type: "topic");
        var queueName = channel.QueueDeclare().QueueName;

        Console.Write("[AGREGADOR] Tópicos a subscrever (ex: sensor.temperatura,sensor.pressao): ");
        string[] topicos = Console.ReadLine().Split(',');

        foreach (var topico in topicos)
        {
            channel.QueueBind(queue: queueName, exchange: "sensores", routingKey: topico);
        }

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"\n[AGREGADOR:{porta}] RabbitMQ → Mensagem recebida: {message}");

            var partes = message.Split(';');
            if (partes.Length == 4)
            {
                string id = partes[0];
                string sensor = partes[1];
                string valor = partes[2];
                string timestamp = partes[3];

                try
                {
                    // ➤ Chamar serviço gRPC de pré-processamento
                    using var channelGrpc = GrpcChannel.ForAddress("http://localhost:5000");
                    var client = new PreprocessService.PreprocessServiceClient(channelGrpc);

                    var request = new PreprocessRequest
                    {
                        Id = id,
                        Sensor = sensor,
                        Valor = valor,
                        Timestamp = timestamp
                    };

                    var resposta = client.Preprocess(request);

                    Console.WriteLine($"[AGREGADOR:{porta}] Dado PRÉ-PROCESSADO: {resposta.Id};{resposta.Sensor};{resposta.Valor};{resposta.Timestamp}");

                    // ➤ Enviar para o servidor via TCP
                    using var servidor = new TcpClient("127.0.0.1", 6000);
                    using var srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };
                    using var srvReader = new StreamReader(servidor.GetStream());

                    srvWriter.WriteLine($"FORWARD_BULK {resposta.Id} 1");
                    string linha = $"{resposta.Sensor};{resposta.Valor};{resposta.Timestamp}";
                    srvWriter.WriteLine(linha);

                    string serverResp = srvReader.ReadLine();
                    Console.WriteLine($"[AGREGADOR:{porta}] Resposta do SERVIDOR: {serverResp}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AGREGADOR:{porta}] Erro no gRPC ou envio ao servidor: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[AGREGADOR:{porta}] Mensagem inválida recebida.");
            }
        };

        channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        Console.WriteLine($"[AGREGADOR:{porta}] Subscrição ativa. A receber dados via RabbitMQ...");
        while (true) Thread.Sleep(1000);
    }

    static void TratarWavy(TcpClient wavy, int porta)
    {
        // Parte antiga baseada em TCP — opcional manter
        StreamReader wavyReader = new StreamReader(wavy.GetStream());
        StreamWriter wavyWriter = new StreamWriter(wavy.GetStream()) { AutoFlush = true };
        string id = "";

        try
        {
            string hello = wavyReader.ReadLine();
            if (hello.StartsWith("HELLO"))
            {
                id = hello.Split(' ')[1];
                wavyWriter.WriteLine("100 OK");

                using var servidor = new TcpClient("127.0.0.1", 6000);
                using var srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };
                using var srvReader = new StreamReader(servidor.GetStream());

                srvWriter.WriteLine("REGISTER " + id);
                string regResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR:{porta}] Resposta do SERVIDOR: {regResp}");
            }

            // Dados por socket TCP (fase antiga)
            string dataBulkHeader = wavyReader.ReadLine();
            if (dataBulkHeader.StartsWith("DATA_BULK"))
            {
                string[] headerParts = dataBulkHeader.Split(' ');
                string wavyId = headerParts[1];
                int n_dados = int.Parse(headerParts[2]);

                string[] dados = new string[n_dados];
                for (int i = 0; i < n_dados; i++)
                {
                    dados[i] = wavyReader.ReadLine();
                }

                using var servidor = new TcpClient("127.0.0.1", 6000);
                using var srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };
                using var srvReader = new StreamReader(servidor.GetStream());

                srvWriter.WriteLine($"FORWARD_BULK {wavyId} {n_dados}");
                foreach (string dado in dados)
                {
                    srvWriter.WriteLine(dado);
                }

                string bulkResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR:{porta}] SERVIDOR respondeu: {bulkResp}");
            }

            string quit = wavyReader.ReadLine();
            if (quit == "QUIT")
            {
                using var servidor = new TcpClient("127.0.0.1", 6000);
                using var srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };
                using var srvReader = new StreamReader(servidor.GetStream());

                srvWriter.WriteLine("DISCONNECT " + id);
                string byeResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR:{porta}] SERVIDOR respondeu: {byeResp}");
            }

            wavyWriter.WriteLine("QUIT_OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGREGADOR:{porta}] Erro: {ex.Message}");
        }
        finally
        {
            wavy.Close();
        }
    }
}
// O código acima implementa um agregador que pode receber dados de sensores via RabbitMQ e TCP.
// Ele processa os dados usando um serviço gRPC de pré-processamento e envia os resultados para um servidor TCP.