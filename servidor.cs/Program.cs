// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

// SERVIDOR/Program.cs
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main()
    {
        int port = 6000;
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"SERVIDOR a escutar na porta {port}...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("AGREGADOR conectado.");

            using NetworkStream stream = client.GetStream();

            string message = ReadMessage(stream);
            Console.WriteLine("<< Recebido: " + message);

            if (message.StartsWith("FORWARD_BULK"))
            {
                // Simula o armazenamento
                File.AppendAllText("dados_recebidos.txt", message + "\n");
                SendMessage(stream, "301 BULK_STORED");
            }
            else if (message == "QUIT")
            {
                SendMessage(stream, "400 BYE");
                Console.WriteLine("Desligar conexão.");
            }
        }
    }

    static string ReadMessage(NetworkStream stream)
    {
        byte[] buffer = new byte[2048];
        int length = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, length).Trim();
    }

    static void SendMessage(NetworkStream stream, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        stream.Write(data, 0, data.Length);
    }
}
