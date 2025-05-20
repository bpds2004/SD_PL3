using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

using System.Text;
using RabbitMQ.Client;

class Wavy
{
    static void Main()
    {
        string ipAgregador = "127.0.0.1";

        Console.Write("[WAVY] Quantas WAVYs pretende criar? ");
        int numWavys = int.Parse(Console.ReadLine());

        // Listas para guardar os dados de configuração
        List<string> ids = new List<string>();
        List<int> portas = new List<int>();

        // Fase de recolha de dados
        for (int i = 1; i <= numWavys; i++)
        {
            Console.WriteLine($"\n[WAVY] --- Configuração da WAVY #{i} ---");

            Console.Write("[WAVY] Introduz o ID da WAVY: ");
            string id = Console.ReadLine();
            ids.Add(id);

            Console.Write("[WAVY] Introduz a PORTA do Agregador: ");
            int porta = int.Parse(Console.ReadLine());
            portas.Add(porta);
        }

        // Fase de execução para cada WAVY
        for (int i = 0; i < numWavys; i++)
        {
            string id = ids[i];
            int porta = portas[i];

            Console.WriteLine();

            try
            {
                // ===================== FASE 2 =====================
                TcpClient client = new TcpClient(ipAgregador, porta);
                StreamReader reader = new StreamReader(client.GetStream());
                StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

                Console.WriteLine($"[WAVY:{id}] Ligado ao Agregador. Enviando HELLO...");
                writer.WriteLine("HELLO " + id);
                string response = reader.ReadLine();
                Console.WriteLine($"[WAVY:{id}] Resposta do AGREGADOR: " + response);

                // ===================== FASE 3 =====================
                Console.WriteLine($"[WAVY:{id}] Iniciando envio de dados de sensores a cada minuto...");

                for (int envio = 1; envio <= 10; envio++) // Envia 10 vezes. Pode ser substituído por: while (true)
                {
                    Console.WriteLine($"[WAVY:{id}] Envio #{envio}");

                    string[] dados = new string[]
                    {
                        "temperatura:22.5",
                        "humidade:45",
                        "pressao:1012"
                    };

                    writer.WriteLine($"DATA_BULK {id} {dados.Length}");

                    foreach (string dado in dados)
                    {
                        Console.WriteLine($"[WAVY:{id}] Enviando: " + dado);
                        writer.WriteLine(dado);
                    }

                    Console.WriteLine($"[WAVY:{id}] A aguardar 1 minuto antes do próximo envio...\n");
                    Thread.Sleep(60000); // Espera 1 minuto (60000 ms)
                }

                // ===================== FASE 4 =====================
                Console.WriteLine($"[WAVY:{id}] Enviando mensagem de desconexão...");
                writer.WriteLine("QUIT");

                string quitResponse = reader.ReadLine();
                Console.WriteLine($"[WAVY:{id}] Resposta do AGREGADOR: " + quitResponse);

                client.Close();
                Console.WriteLine($"[WAVY:{id}] Conexão encerrada.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WAVY:{id}] Erro: {ex.Message}");
            }
        }

        Console.WriteLine("\n[WAVY] Todas as WAVYs terminaram. Pressiona Enter para sair...");
        Console.ReadLine();
    }

    static string ObterValor(string[] dados, string sensor)
    {
        foreach (string linha in dados)
        {
            if (linha.StartsWith(sensor + ":"))
            {
                return linha.Split(':')[1];
            }
        }
        return "";
    }
}

