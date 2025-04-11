// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

/*//WAVY FASE3
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
FASE2 */
using System;
using System.IO;
using System.Net.Sockets;

class Wavy
{
    static void Main()
    {
        Console.Write("[WAVY] Introduz o ID da WAVY: ");
        string id = Console.ReadLine();

        TcpClient client = new TcpClient("127.0.0.1", 5000);
        StreamReader reader = new StreamReader(client.GetStream());
        StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

        writer.WriteLine("HELLO " + id);
        string response = reader.ReadLine();
        Console.WriteLine("[WAVY] Resposta do AGREGADOR: " + response);

        // ====== DADOS PARA A FASE 3 ======
        // Simular envio de dados (podes adaptar isto mais tarde)
        string[] dados = new string[]
        {
            "temperatura:22.5",
            "humidade:45",
            "pressao:1012"
        };

        writer.WriteLine($"DATA_BULK {id} {dados.Length}");
        foreach (string dado in dados)
        {
            writer.WriteLine(dado);
        }

        // Espera para encerrar
        Console.WriteLine("[WAVY] Pressiona Enter para sair...");
        Console.ReadLine();

        writer.WriteLine("QUIT");
        client.Close();
    }
}
