using System.Net.Sockets;
using System.Net;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

class Servidor
{
    // Objeto de bloqueio para escrita segura no ficheiro
    static readonly object lockFicheiro = new object();

    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 6000);
        listener.Start();
        Console.WriteLine("[SERVIDOR] A ouvir na porta 6000...");

        // Criar ficheiro CSV com cabeçalho se ainda não existir
        string ficheiroCsv = "dados.csv";
        if (!File.Exists(ficheiroCsv))
        {
            using (StreamWriter sw = new StreamWriter(ficheiroCsv, append: false))
            {
                sw.WriteLine("id_wavy,sensor,valor");
            }
        }

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

                // Escreve todos os dados num único ficheiro CSV
                lock (lockFicheiro)
                {
                    using (StreamWriter sw = new StreamWriter("dados.csv", append: true))
                    {
                        foreach (string linha in dados)
                        {
                            string[] partes = linha.Split(':');
                            if (partes.Length == 2)
                            {
                                string sensor = partes[0];
                                string valor = partes[1];
                                sw.WriteLine($"{id},{sensor},{valor}");
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
}
