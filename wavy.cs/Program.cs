// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using System;
using System.Net.Sockets;
using System.Text;

class Wavy
{
    static void Main()
    {
        string wavyId = "WAVY123";
        string ip = "127.0.0.1";
        int port = 5000;

        using TcpClient client = new TcpClient(ip, port);
        using NetworkStream stream = client.GetStream();

        Enviar(stream, $"HELLO {wavyId}");
        Console.WriteLine("<< " + Ler(stream));

        Enviar(stream, $"DATA_BULK {wavyId}\n2\ntemperatura:22.5\nsalinidade:36.1");
        Console.WriteLine("<< " + Ler(stream));

        Enviar(stream, "QUIT");
        Console.WriteLine("<< " + Ler(stream));
    }

    static void Enviar(NetworkStream stream, string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
        stream.Write(data, 0, data.Length);
    }

    static string Ler(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        int length = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, length).Trim();
    }
}