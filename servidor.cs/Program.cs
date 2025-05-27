using System.Net.Sockets;
using System.Net;
using System;
using System.IO;
using System.Threading;
using Grpc.Net.Client;
using Analyze;

class Servidor
{
    static readonly object lockFicheiro = new object();

    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 6000);
        listener.Start();
        Console.WriteLine("[SERVIDOR] A ouvir na porta 6000...");

        string ficheiroCsv = "dados.csv";
        if (!File.Exists(ficheiroCsv))
        {
            using (StreamWriter sw = new StreamWriter(ficheiroCsv, append: false))
            {
                sw.WriteLine("id_wavy,sensor,valor,hora");
            }
        }

        // Thread para pedidos de análise
        new Thread(() => MenuAnalise()).Start();

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread t = new Thread(() => TratarCliente(client));
            t.Start();
        }
    }

    static void TratarCliente(TcpClient client)
    {
        Console.WriteLine("[SERVIDOR] Ligado ao AGREGADOR.");
        StreamReader reader = new StreamReader(client.GetStream());
        StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

        try
        {
            string msg = reader.ReadLine();
            Console.WriteLine($"[SERVIDOR] Recebido: {msg}");

            if (msg.StartsWith("REGISTER"))
            {
                writer.WriteLine("200 REGISTERED");
            }
            else if (msg.StartsWith("DISCONNECT"))
            {
                writer.WriteLine("400 BYE");
            }
            else if (msg.StartsWith("FORWARD_BULK"))
            {
                string[] parts = msg.Split(' ');
                string id = parts[1];
                int n_dados = int.Parse(parts[2]);

                string[] dados = new string[n_dados];
                for (int i = 0; i < n_dados; i++)
                {
                    dados[i] = reader.ReadLine();
                }

                Console.WriteLine($"[SERVIDOR] Dados recebidos da WAVY {id}:");
                foreach (string d in dados)
                {
                    Console.WriteLine("  " + d);
                }

                lock (lockFicheiro)
                {
                    using (StreamWriter sw = new StreamWriter("dados.csv", append: true))
                    {
                        foreach (string linha in dados)
                        {
                            string[] partes = linha.Split(';');
                            if (partes.Length == 4)
                            {
                                string sensor = partes[1];
                                string valor = partes[2];
                                string hora = partes[3];
                                sw.WriteLine($"{id},{sensor},{valor},{hora}");
                            }
                        }
                    }
                }

                writer.WriteLine("301 BULK_STORED");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[SERVIDOR] Erro: " + ex.Message);
        }
        finally
        {
            client.Close();
        }
    }

    static void MenuAnalise()
    {
        while (true)
        {
            Console.WriteLine("\n[SERVIDOR] Introduza o nome do sensor para análise (ex: temperatura), ou ENTER para ignorar:");
            string sensor = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(sensor)) continue;

            try
            {
                using var channel = GrpcChannel.ForAddress("http://localhost:5000");
                var client = new AnalysisService.AnalysisServiceClient(channel);

                var resposta = client.Analyze(new AnalyzeRequest { Sensor = sensor });
                Console.WriteLine($"[SERVIDOR] Análise RPC: Sensor = {sensor}, Média = {resposta.Average:F2}, Total = {resposta.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SERVIDOR] Erro ao contactar serviço de análise: " + ex.Message);
            }
        }
    }
}