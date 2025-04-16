using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Servidor
{
    // ===================== FASE 4 =====================
    // Objeto de bloqueio para escrita em ficheiro
    static readonly object lockFicheiro = new object();

    static void Main()
    {
        // Inicia servidor na porta 6000
        TcpListener listener = new TcpListener(IPAddress.Any, 6000);
        listener.Start();
        Console.WriteLine("[SERVIDOR] A ouvir na porta 6000...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();

            // ===================== FASE 4 =====================
            // Nova thread para tratar cada cliente
            Thread t = new Thread(() => TratarCliente(client));
            t.Start();
        }
    }

    static void TratarCliente(TcpClient client)
    {
        Console.WriteLine("[SERVIDOR] Ligado ao AGREGADOR.");
        StreamReader reader = new StreamReader(client.GetStream());
        StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

        // Lê mensagem recebida
        string msg = reader.ReadLine();
        Console.WriteLine($"[SERVIDOR] Recebido: {msg}");

        if (msg.StartsWith("REGISTER"))
        {
            // ===================== FASE 2 =====================
            // Responde ao pedido de registo
            writer.WriteLine("200 REGISTERED");
        }
        else if (msg.StartsWith("DISCONNECT"))
        {
            // ===================== FASE 4 =====================
            // Responde ao pedido de desconexão
            writer.WriteLine("400 BYE");
        }
        // ===================== FASE 3 =====================
        else if (msg.StartsWith("FORWARD_BULK"))
        {
            string[] parts = msg.Split(' ');
            string id = parts[1];
            int n_dados = int.Parse(parts[2]);

            // Lê os dados recebidos
            string[] dados = new string[n_dados];
            for (int i = 0; i < n_dados; i++)
            {
                dados[i] = reader.ReadLine();
            }

            // Mostra os dados no terminal
            Console.WriteLine($"[SERVIDOR] Dados recebidos da WAVY {id}:");
            foreach (string d in dados)
            {
                Console.WriteLine("  " + d);
            }

            // ===================== FASE 4 =====================
            // Protege acesso concorrente ao ficheiro
            lock (lockFicheiro)
            {
                string ficheiroTxt = $"dados_{id}.txt";
                using (StreamWriter sw = new StreamWriter(ficheiroTxt, append: true))
                {
                    foreach (string linha in dados)
                    {
                        sw.WriteLine(linha);
                    }
                }
            }

            // Responde ao Agregador
            writer.WriteLine("301 BULK_STORED");
        }

        client.Close(); // Fecha ligação
    }
}
