using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

class Wavy
{
    static void Main()
    {
        string ipAgregador = "127.0.0.1";

        /*–– colecção viva de WAVYs ––*/
        var wavys = new List<(string id, int porta,
                              TcpClient cli, StreamReader rd, StreamWriter wr)>();

        while (true)
        {
            Console.Write("[WAVY] Quantas WAVYs pretende criar? (clique 'q' para sair) ");
            string inp = Console.ReadLine();
            if (inp.Trim().Equals("q", StringComparison.OrdinalIgnoreCase))
                break;

            int n = string.IsNullOrWhiteSpace(inp) ? 0 : int.Parse(inp);

            /*–– criar as WAVYs pedidas ––*/
            for (int i = 0; i < n; i++)
            {
                Console.WriteLine($"\n[WAVY] --- Configuração da WAVY #{i} ---");
                Console.Write("[WAVY] Introduz o ID da WAVY: ");
                string id = Console.ReadLine();
                Console.Write("[WAVY] Introduz a PORTA do Agregador: ");
                int porta = int.Parse(Console.ReadLine());

                try
                {
                    var cli = new TcpClient(ipAgregador, porta);
                    var rd = new StreamReader(cli.GetStream());
                    var wr = new StreamWriter(cli.GetStream()) { AutoFlush = true };

                    /*–– FASE 2 : HELLO ––*/
                    wr.WriteLine($"HELLO {id}");
                    Console.WriteLine($"[WAVY:{id}] {rd.ReadLine()}");   // 100 OK

                    wavys.Add((id, porta, cli, rd, wr));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WAVY] Erro a ligar: {ex.Message}");
                }
            }

            /*–– envia pacote fixo a TODAS as WAVYs ––*/
            foreach (var w in wavys.ToArray())   // snapshot
            {
                try
                {
                    // === FASE 3 : DATA_BULK ===
                    string[] leituras =
                    {
                        "temperatura:22.5",
                        "humidade:45",
                        "pressao:1012"
                    };

                    w.wr.WriteLine($"DATA_BULK {w.id} {leituras.Length}");
                    foreach (var l in leituras) w.wr.WriteLine(l);

                    /*–– única resposta por pacote ––*/
                    Console.WriteLine($"[WAVY:{w.id}] {w.rd.ReadLine()}");   // 301 BULK_STORED
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WAVY:{w.id}] erro: {ex.Message}");
                    wavys.Remove(w);               // retira defunta
                }
            }

            Thread.Sleep(40_000);   // 40 seg
        }

        /*–– FASE 4 : QUIT ordenado ––*/
        foreach (var w in wavys)
        {
            try
            {
                w.wr.WriteLine("QUIT");
                Console.WriteLine($"[WAVY:{w.id}] {w.rd.ReadLine()}"); // QUIT_OK
                w.cli.Close();
            }
            catch { /*ignorar*/ }
        }
        Console.WriteLine("[INFO] Premir Enter para fechar…");
        Console.ReadLine();
    }
}
