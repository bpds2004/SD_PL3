//using System;                      // Importa funcionalidades básicas como Console
//using System.IO;                   // Permite ler e escrever em streams
//using System.Net.Sockets;         // Necessário para comunicação TCP/IP

//class Wavy
//{
//    static void Main()
//    {
//        // Pede ao utilizador o ID da WAVY
//        Console.Write("[WAVY] Introduz o ID da WAVY: ");
//        string id = Console.ReadLine();  // Lê o ID introduzido

//        // ===================== FASE 2 =====================
//        // Cria ligação ao Agregador na porta 5000
//        TcpClient client = new TcpClient("127.0.0.1", 5000);
//        // Cria leitor e escritor para o stream de comunicação
//        StreamReader reader = new StreamReader(client.GetStream());
//        StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

//        // Envia mensagem de apresentação (HELLO)
//        writer.WriteLine("HELLO " + id);
//        // Aguarda e imprime a resposta do Agregador
//        string response = reader.ReadLine();
//        Console.WriteLine("[WAVY] Resposta do AGREGADOR: " + response);


//        // ===================== FASE 3 =====================
//        // Simula dados sensoriais
//        string[] dados = new string[]
//        {
//            "temperatura:22.5",
//            "humidade:45",
//            "pressao:1012"
//        };

//        // Envia cabeçalho com o número de dados
//        writer.WriteLine($"DATA_BULK {id} {dados.Length}");

//        // Envia cada linha de dados
//        foreach (string dado in dados)
//        {
//            writer.WriteLine(dado);
//        }

//        // Aguarda input antes de encerrar
//        Console.WriteLine("[WAVY] Pressiona Enter para sair...");
//        Console.ReadLine();

//        // Envia mensagem de encerramento
//        writer.WriteLine("QUIT");
//        client.Close(); // Fecha ligação TCP
//    }
//}
using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;

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
                Console.WriteLine($"[WAVY:{id}] Enviando dados de sensores...");
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

                // Aguarda input antes de encerrar
                Console.WriteLine($"[WAVY:{id}] Pressiona Enter para continuar...");
                Console.ReadLine();

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
}
