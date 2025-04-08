// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

// FASE3/AGREGADOR/Program.cs
/*using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main()
    {
        int port = 5000;
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"AGREGADOR a escutar na porta {port}...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("WAVY conectada.");

            using NetworkStream stream = client.GetStream();

            string received;
            while ((received = ReadMessage(stream)) != null)
            {
                Console.WriteLine("<< Recebido: " + received);

                if (received.StartsWith("HELLO"))
                {
                    SendMessage(stream, "100 OK");
                }
                else if (received.StartsWith("DATA_BULK"))
                {
                    // Opcional: guardar localmente
                    string[] parts = received.Split('\n');
                    string header = parts[0];
                    string dataContent = string.Join('\n', parts[1..]);

                    // Encaminhar para o SERVIDOR
                    ForwardToServidor(header + "\n" + dataContent);
                }
                else if (received.StartsWith("QUIT"))
                {
                    Console.WriteLine("Conexão finalizada.");
                    break;
                }
            }
        }
    }

    static string ReadMessage(NetworkStream stream)
    {
        byte[] buffer = new byte[2048];
        int length = stream.Read(buffer, 0, buffer.Length);
        if (length == 0) return null;
        return Encoding.UTF8.GetString(buffer, 0, length).Trim();
    }

    static void SendMessage(NetworkStream stream, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        stream.Write(data, 0, data.Length);
    }

    static void ForwardToServidor(string data)
    {
        string serverIp = "127.0.0.1";
        int serverPort = 6000;

        using TcpClient client = new TcpClient(serverIp, serverPort);
        using NetworkStream stream = client.GetStream();

        SendMessage(stream, $"FORWARD_BULK {data}");
        Console.WriteLine(">> Encaminhado para SERVIDOR");

        string response = ReadMessage(stream);
        Console.WriteLine("<< Resposta do SERVIDOR: " + response);
    }
}
FASE2*/
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

class Agregador
{
    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("[AGREGADOR] À espera da WAVY na porta 5000...");

        while (true)
        {
            TcpClient wavy = listener.AcceptTcpClient();
            Console.WriteLine("[AGREGADOR] WAVY conectada.");
            StreamReader wavyReader = new StreamReader(wavy.GetStream());
            StreamWriter wavyWriter = new StreamWriter(wavy.GetStream()) { AutoFlush = true };

            string hello = wavyReader.ReadLine();
            Console.WriteLine($"[AGREGADOR] Recebido da WAVY: {hello}");
            if (hello.StartsWith("HELLO"))
            {
                string id = hello.Split(' ')[1];
                wavyWriter.WriteLine("100 OK");

                TcpClient servidor = new TcpClient("127.0.0.1", 6000);
                StreamReader srvReader = new StreamReader(servidor.GetStream());
                StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

                srvWriter.WriteLine("REGISTER " + id);
                string regResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR] Resposta do SERVIDOR: {regResp}");

                servidor.Close();
            }

            string quit = wavyReader.ReadLine();
            Console.WriteLine($"[AGREGADOR] WAVY diz: {quit}");
            if (quit == "QUIT")
            {
                string id = hello.Split(' ')[1];
                TcpClient servidor = new TcpClient("127.0.0.1", 6000);
                StreamReader srvReader = new StreamReader(servidor.GetStream());
                StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

                srvWriter.WriteLine("DISCONNECT " + id);
                string byeResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR] SERVIDOR respondeu: {byeResp}");

                servidor.Close();
            }

            wavy.Close();
        }
    }
}


