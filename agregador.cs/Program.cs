//using System;
//using System.IO;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;

//class Agregador
//{
//    static void Main()
//    {
//        // Inicia o listener TCP na porta 5000
//        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
//        listener.Start();
//        Console.WriteLine("[AGREGADOR] À espera da WAVY na porta 5000...");

//        while (true)
//        {
//            // Aceita nova ligação de uma WAVY
//            TcpClient wavy = listener.AcceptTcpClient();

//            // ===================== FASE 4 =====================
//            // Inicia uma nova thread para tratar essa WAVY
//            Thread t = new Thread(() => TratarWavy(wavy));
//            t.Start();
//        }
//    }

//    static void TratarWavy(TcpClient wavy)
//    {
//        Console.WriteLine("[AGREGADOR] WAVY conectada.");
//        StreamReader wavyReader = new StreamReader(wavy.GetStream());
//        StreamWriter wavyWriter = new StreamWriter(wavy.GetStream()) { AutoFlush = true };

//        // ===================== FASE 2 =====================
//        // Lê a mensagem HELLO da WAVY
//        string hello = wavyReader.ReadLine();
//        Console.WriteLine($"[AGREGADOR] Recebido da WAVY: {hello}");
//        string id = "";

//        if (hello.StartsWith("HELLO"))
//        {
//            // Extrai o ID
//            id = hello.Split(' ')[1];
//            // Envia resposta de confirmação
//            wavyWriter.WriteLine("100 OK");

//            // Conecta ao servidor para registar a WAVY
//            TcpClient servidor = new TcpClient("127.0.0.1", 6000);
//            StreamReader srvReader = new StreamReader(servidor.GetStream());
//            StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

//            // Envia pedido de registo
//            srvWriter.WriteLine("REGISTER " + id);
//            // Lê resposta do servidor
//            string regResp = srvReader.ReadLine();
//            Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {regResp}");

//            servidor.Close();
//        }
//        // ===================== FIM FASE 2 =====================

//        // ===================== FASE 3 =====================
//        // Lê cabeçalho do envio de dados
//        string dataBulkHeader = wavyReader.ReadLine();
//        if (dataBulkHeader.StartsWith("DATA_BULK"))
//        {
//            // Divide o cabeçalho em partes
//            string[] headerParts = dataBulkHeader.Split(' ');
//            string wavyId = headerParts[1];
//            int n_dados = int.Parse(headerParts[2]);

//            // Lê os dados um a um
//            string[] dados = new string[n_dados];
//            for (int i = 0; i < n_dados; i++)
//            {
//                dados[i] = wavyReader.ReadLine();
//            }

//            // Reencaminha os dados ao servidor
//            TcpClient servidor = new TcpClient("127.0.0.1", 6000);
//            StreamReader srvReader = new StreamReader(servidor.GetStream());
//            StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

//            // Envia cabeçalho
//            srvWriter.WriteLine($"FORWARD_BULK {wavyId} {n_dados}");
//            // Envia dados
//            foreach (string dado in dados)
//            {
//                srvWriter.WriteLine(dado);
//            }

//            // Lê resposta
//            string bulkResp = srvReader.ReadLine();
//            Console.WriteLine($"[AGREGADOR] SERVIDOR respondeu: {bulkResp}");

//            servidor.Close();
//        }
//        // ===================== FIM FASE 3 =====================

//        // Lê mensagem de encerramento da WAVY
//        string quit = wavyReader.ReadLine();
//        Console.WriteLine($"[AGREGADOR] WAVY diz: {quit}");
//        if (quit == "QUIT")
//        {
//            // ===================== FASE 4 =====================
//            // Envia desconexão ao servidor
//            TcpClient servidor = new TcpClient("127.0.0.1", 6000);
//            StreamReader srvReader = new StreamReader(servidor.GetStream());
//            StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

//            srvWriter.WriteLine("DISCONNECT " + id);
//            string byeResp = srvReader.ReadLine();
//            Console.WriteLine($"[AGREGADOR] SERVIDOR respondeu: {byeResp}");

//            servidor.Close();
//        }

//        wavy.Close(); // Fecha ligação da WAVY
//    }
//}
//using System;
//using System.IO;
//using System.Net;
//using System.Net.Sockets;
//using System.Threading;

//class Agregador
//{
//    static void Main()
//    {
//        Console.Write("Quantos Agregadores deseja iniciar? ");
//        int numAgregadores = int.Parse(Console.ReadLine());

//        for (int i = 1; i <= numAgregadores; i++)
//        {
//            Console.Write($"Porta para o Agregador #{i}: ");
//            int porta = int.Parse(Console.ReadLine());

//            // Cria uma thread para cada agregador
//            int portaCapturada = porta;
//            new Thread(() => IniciarAgregador(portaCapturada)).Start();
//        }

//        Console.WriteLine("Todos os Agregadores estão à escuta! Pressiona Enter para sair...");
//        Console.ReadLine();
//    }

//    static void IniciarAgregador(int porta)
//    {
//        TcpListener listener = new TcpListener(IPAddress.Any, porta);
//        listener.Start();
//        Console.WriteLine($"[AGREGADOR:{porta}] A escutar na porta {porta}...");

//        while (true)
//        {
//            TcpClient client = listener.AcceptTcpClient();
//            Console.WriteLine($"[AGREGADOR:{porta}] Ligação recebida.");

//            new Thread(() => LidarComCliente(client, porta)).Start();
//        }
//    }

//    static void LidarComCliente(TcpClient client, int porta)
//    {
//        StreamReader reader = new StreamReader(client.GetStream());
//        StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

//        string linha;
//        string id = "";

//        while ((linha = reader.ReadLine()) != null)
//        {
//            Console.WriteLine($"[AGREGADOR:{porta}] Recebido: {linha}");

//            if (linha.StartsWith("HELLO"))
//            {
//                id = linha.Split(' ')[1];
//                writer.WriteLine("HELLO_OK");
//            }
//            else if (linha.StartsWith("DATA_BULK"))
//            {
//                string[] partes = linha.Split(' ');
//                int numDados = int.Parse(partes[2]);

//                Console.WriteLine($"[AGREGADOR:{porta}] A receber {numDados} dados de {id}:");
//                for (int i = 0; i < numDados; i++)
//                {
//                    string dado = reader.ReadLine();
//                    Console.WriteLine($"  -> {dado}");
//                }
//            }
//            else if (linha == "QUIT")
//            {
//                writer.WriteLine("QUIT_OK");
//                Console.WriteLine($"[AGREGADOR:{porta}] {id} desconectado.");
//                break;
//            }
//        }

//        client.Close();
//    }
//}

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Agregador
{
    static void Main()
    {
        Console.Write("[AGREGADOR] Quantos Agregadores deseja iniciar? ");
        int numAgregadores = int.Parse(Console.ReadLine());

        int[] portas = new int[numAgregadores];

        // Primeiro, recolhe todas as portas
        for (int i = 0; i < numAgregadores; i++)
        {
            Console.Write($"[AGREGADOR] Porta para o Agregador #{i + 1}: ");
            portas[i] = int.Parse(Console.ReadLine());
        }

        // Depois, inicia cada Agregador
        foreach (int porta in portas)
        {
            new Thread(() => IniciarAgregador(porta)).Start();
        }

        Console.WriteLine("\n[AGREGADOR] Todos os Agregadores estão à escuta! Pressiona Enter para sair...");
        Console.ReadLine();
    }

    static void IniciarAgregador(int porta)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, porta);
        listener.Start();
        Console.WriteLine($"[AGREGADOR:{porta}] À espera de WAVYs na porta {porta}...");

        while (true)
        {
            TcpClient wavy = listener.AcceptTcpClient();
            new Thread(() => TratarWavy(wavy, porta)).Start();
        }
    }

    static void TratarWavy(TcpClient wavy, int porta)
    {
        Console.WriteLine($"[AGREGADOR:{porta}] WAVY conectada.");
        StreamReader wavyReader = new StreamReader(wavy.GetStream());
        StreamWriter wavyWriter = new StreamWriter(wavy.GetStream()) { AutoFlush = true };

        string id = "";

        try
        {
            // === FASE 2: HELLO ===
            string hello = wavyReader.ReadLine();
            Console.WriteLine($"[AGREGADOR:{porta}] Recebido da WAVY: {hello}");

            if (hello.StartsWith("HELLO"))
            {
                id = hello.Split(' ')[1];
                wavyWriter.WriteLine("100 OK");

                TcpClient servidor = new TcpClient("127.0.0.1", 6000);
                StreamReader srvReader = new StreamReader(servidor.GetStream());
                StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

                Console.WriteLine($"[AGREGADOR:{porta}] A registar ID '{id}' no SERVIDOR...");
                srvWriter.WriteLine("REGISTER " + id);
                string regResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR:{porta}] Resposta do SERVIDOR: {regResp}");

                servidor.Close();
            }

            // === FASE 3: Dados ===
            string dataBulkHeader = wavyReader.ReadLine();
            Console.WriteLine($"[AGREGADOR:{porta}] Cabeçalho recebido: {dataBulkHeader}");
            if (dataBulkHeader.StartsWith("DATA_BULK"))
            {
                string[] headerParts = dataBulkHeader.Split(' ');
                string wavyId = headerParts[1];
                int n_dados = int.Parse(headerParts[2]);

                string[] dados = new string[n_dados];
                for (int i = 0; i < n_dados; i++)
                {
                    dados[i] = wavyReader.ReadLine();
                    Console.WriteLine($"[AGREGADOR:{porta}] Dado recebido: {dados[i]}");
                }

                TcpClient servidor = new TcpClient("127.0.0.1", 6000);
                StreamReader srvReader = new StreamReader(servidor.GetStream());
                StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

                Console.WriteLine($"[AGREGADOR:{porta}] A reenviar dados ao SERVIDOR...");
                srvWriter.WriteLine($"FORWARD_BULK {wavyId} {n_dados}");
                foreach (string dado in dados)
                {
                    srvWriter.WriteLine(dado);
                }

                string bulkResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR:{porta}] SERVIDOR respondeu: {bulkResp}");

                servidor.Close();
            }

            // === FASE 4: QUIT ===
            string quit = wavyReader.ReadLine();
            Console.WriteLine($"[AGREGADOR:{porta}] WAVY diz: {quit}");

            if (quit == "QUIT")
            {
                TcpClient servidor = new TcpClient("127.0.0.1", 6000);
                StreamReader srvReader = new StreamReader(servidor.GetStream());
                StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

                Console.WriteLine($"[AGREGADOR:{porta}] A informar desconexão do ID {id} ao SERVIDOR...");
                srvWriter.WriteLine("DISCONNECT " + id);
                string byeResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR:{porta}] SERVIDOR respondeu: {byeResp}");

                servidor.Close();
            }

            wavyWriter.WriteLine("QUIT_OK");
            Console.WriteLine($"[AGREGADOR:{porta}] {id} desconectado.");
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
