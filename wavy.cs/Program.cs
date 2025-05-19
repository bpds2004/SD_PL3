/*
 * WAVY
 * ----
 * – Corre tantas WAVYs quantas o utilizador quiser, criando‑as em ciclos de 40 s.
 * – Mantém cada ligação aberta: envia HELLO uma vez, DATA_BULK de 40 em 40 s,
 *   e só envia QUIT quando o utilizador finalmente escreve ‘q’.
 * – Guarda localmente (apenas para DEBUG) o CSV recebido do Servidor.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

class Wavy
{
    /* -------- configuração -------- */
    const string IP_AGREGADOR = "127.0.0.1";
    const int INTERVALO_MS = 40_000;          // 40 s

    static void Main()
    {
        /* Colecção das WAVYs vivas.
           Cada tuplo guarda: id ‑ porta ‑ tcp ‑ reader ‑ writer */
        var wavys = new List<(string id, int port,
                              TcpClient cli, StreamReader rd, StreamWriter wr)>();

        while (true)
        {
            Console.Write("[WAVY] Quantas WAVYs pretende criar? (clique 'q' para sair) ");
            string inp = Console.ReadLine()?.Trim();

            if (inp.Equals("q", StringComparison.OrdinalIgnoreCase))
                break;                                  // sai do ciclo

            /* Se o utilizador carregar só <Enter>, não cria nenhuma – continua para enviar */
            int nNovas = string.IsNullOrEmpty(inp) ? 0 : int.Parse(inp);

            /* ------------------- criação de novas WAVYs ------------------- */
            for (int i = 0; i < nNovas; i++)
            {
                Console.WriteLine($"\n[WAVY] --- Configuração da WAVY #{i + 1} ---");
                Console.Write("[WAVY] Introduz o ID da WAVY: ");
                string id = Console.ReadLine()!.Trim();
                Console.Write("[WAVY] Introduz a PORTA do Agregador: ");
                int port = int.Parse(Console.ReadLine()!);

                try
                {
                    var cli = new TcpClient(IP_AGREGADOR, port);
                    var rd = new StreamReader(cli.GetStream());
                    var wr = new StreamWriter(cli.GetStream()) { AutoFlush = true };

                    /* -------- FASE 2 : HELLO -------- */
                    Console.WriteLine($"[WAVY:{id}] Ligado ao Agregador. Enviando HELLO...");
                    wr.WriteLine($"HELLO {id}");
                    Console.WriteLine($"[WAVY:{id}] Resposta do AGREGADOR: {rd.ReadLine()}");

                    wavys.Add((id, port, cli, rd, wr));     // regista a WAVY viva
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WAVY] Erro a ligar: {ex.Message}");
                }
            }

            /* ------------------- envio periódico para TODAS ------------------- */
            foreach (var w in wavys.ToArray())             // snapshot p/ não alterar enumeração
            {
                try
                {
                    /* -------- FASE 3 : DATA_BULK -------- */
                    string[] dados =
                    {
                        "temperatura:22.5",
                        "humidade:45",
                        "pressao:1012"
                    };

                    Console.WriteLine($"\n[WAVY:{w.id}] Enviando dados de sensores...");
                    w.wr.WriteLine($"DATA_BULK {w.id} {dados.Length}");
                    foreach (string l in dados)
                    {
                        Console.WriteLine($"[WAVY:{w.id}] Enviando: {l}");
                        w.wr.WriteLine(l);
                    }

                    /* ---- confirma única resposta ---- */
                    Console.WriteLine($"[WAVY:{w.id}] {w.rd.ReadLine()}");   // 301 BULK_STORED
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WAVY:{w.id}] erro: {ex.Message}");
                    wavys.Remove(w);                       // elimina defunta
                }
            }

            Thread.Sleep(INTERVALO_MS);                    // espera 40 s e volta a perguntar
        }

        /* ------------------- FASE 4 : QUIT ordenado ------------------- */
        foreach (var w in wavys)
        {
            try
            {
                Console.WriteLine($"\n[WAVY:{w.id}] Enviando mensagem de desconexão...");
                w.wr.WriteLine("QUIT");
                Console.WriteLine($"[WAVY:{w.id}] Resposta do AGREGADOR: {w.rd.ReadLine()}");
                w.cli.Close();
            }
            catch { /* ignora falhas na limpeza */ }
        }

        Console.WriteLine("[INFO] Premir Enter para fechar…");
        Console.ReadLine();
    }
}
