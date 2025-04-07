// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

// AGREGADOR/Program.cs
using System;
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
