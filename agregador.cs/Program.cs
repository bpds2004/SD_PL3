/*
 * AGREGADOR
 * ---------
 * – Aceita criar vários listeners em portas diferentes.
 * – Cada ligação de WAVY é tratada em thread própria:
 *       HELLO  -> regista no Servidor
 *       DATA_BULK -> envia FORWARD_BULK ao Servidor, devolve 301
 *       QUIT  -> notifica DISCONNECT, devolve QUIT_OK
 */

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Agregador
{
    static readonly ConcurrentDictionary<int, TcpListener> listeners = new();   // porta → listener

    static void Main()
    {
        while (true)
        {
            Console.Write("[AGREGADOR] Quantos Agregadores pretende criar? (clique 'q' para sair) ");
            string inp = Console.ReadLine()?.Trim();
            if (inp.Equals("q", StringComparison.OrdinalIgnoreCase)) break;

            int n = string.IsNullOrEmpty(inp) ? 0 : int.Parse(inp);

            /* -------- cria os listeners pedidos -------- */
            for (int i = 0; i < n; i++)
            {
                Console.Write($"[AGREGADOR]   Porta para o Agregador #{i + 1}: ");
                int port = int.Parse(Console.ReadLine()!);

                if (listeners.ContainsKey(port))
                {
                    Console.WriteLine("[AGREGADOR] Já existe listener nessa porta.");
                    continue;
                }

                var lst = new TcpListener(IPAddress.Any, port);
                lst.Start();
                listeners[port] = lst;
                new Thread(() => Escutar(lst, port)).Start();
                Console.WriteLine($"[AGREGADOR:{port}] À espera de WAVYs na porta {port}...");
            }

            Thread.Sleep(40_000);                        // pausa meramente para ritmo
        }

        /* -------- shutdown global -------- */
        foreach (var kv in listeners) kv.Value.Stop();
    }

    /* ========================================================= */

    static void Escutar(TcpListener lst, int port)
    {
        try
        {
            while (true)
            {
                var cli = lst.AcceptTcpClient();          // bloqueia até haver ligação
                new Thread(() => TratarWavy(cli, port)).Start();
            }
        }
        catch (SocketException) { /* listener fechado; sai */ }
    }

    static void TratarWavy(TcpClient wavy, int port)
    {
        Console.WriteLine($"[AGREGADOR:{port}] WAVY conectada.");
        var r = new StreamReader(wavy.GetStream());
        var w = new StreamWriter(wavy.GetStream()) { AutoFlush = true };
        string id = "";

        try
        {
            /* -------- FASE 2 : HELLO -------- */
            string hello = r.ReadLine();
            Console.WriteLine($"[AGREGADOR:{port}] Recebido da WAVY: {hello}");

            if (hello.StartsWith("HELLO"))
            {
                id = hello.Split(' ')[1];
                w.WriteLine("100 OK");

                Console.WriteLine($"[AGREGADOR:{port}] A registar ID '{id}' no SERVIDOR...");
                Console.WriteLine($"[AGREGADOR:{port}] Resposta do SERVIDOR: {EnviarServidor($"REGISTER {id}")}");
            }

            /* -------- ciclo principal -------- */
            while (true)
            {
                string header = r.ReadLine();             // DATA_BULK … ou QUIT
                if (header == null) break;

                /* ------- FASE 3 : DATA_BULK ------- */
                if (header.StartsWith("DATA_BULK"))
                {
                    Console.WriteLine($"[AGREGADOR:{port}] Cabeçalho recebido: {header}");
                    string[] h = header.Split(' ');
                    string wavyId = h[1];
                    int n = int.Parse(h[2]);

                    string[] dados = new string[n];
                    for (int i = 0; i < n; i++)
                    {
                        dados[i] = r.ReadLine();
                        Console.WriteLine($"[AGREGADOR:{port}] Dado recebido: {dados[i]}");
                    }

                    Console.WriteLine($"[AGREGADOR:{port}] A reenviar dados ao SERVIDOR.");
                    string resp = EnviarServidorBulk(wavyId, port, dados);
                    Console.WriteLine($"[AGREGADOR:{port}] SERVIDOR respondeu: {resp}");
                    w.WriteLine(resp);                    // devolve 301 BULK_STORED
                    continue;
                }

                /* ------- FASE 4 : QUIT ------- */
                if (header == "QUIT")
                {
                    Console.WriteLine($"[AGREGADOR:{port}] WAVY diz: QUIT");
                    Console.WriteLine($"[AGREGADOR:{port}] A informar desconexão do ID {id} ao SERVIDOR...");
                    Console.WriteLine($"[AGREGADOR:{port}] SERVIDOR respondeu: {EnviarServidor($"DISCONNECT {id}")}");
                    w.WriteLine("QUIT_OK");
                    Console.WriteLine($"[AGREGADOR:{port}] {id} desconectado.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGREGADOR:{port}] Erro: {ex.Message}");
        }
        finally
        {
            wavy.Close();
        }
    }

    /* ========================================================= */

    /* –– pedido simples (REGISTER / DISCONNECT) –– */
    static string EnviarServidor(string msg)
    {
        using var srv = new TcpClient("127.0.0.1", 6000);
        var r = new StreamReader(srv.GetStream());
        var w = new StreamWriter(srv.GetStream()) { AutoFlush = true };
        w.WriteLine(msg);
        return r.ReadLine();            // 200 REGISTERED ou 400 BYE
    }

    /* –– pedido de bulk –– */
    static string EnviarServidorBulk(string id, int port, string[] dados)
    {
        using var srv = new TcpClient("127.0.0.1", 6000);
        var r = new StreamReader(srv.GetStream());
        var w = new StreamWriter(srv.GetStream()) { AutoFlush = true };

        w.WriteLine($"FORWARD_BULK {id} {port} {dados.Length}");
        foreach (string d in dados) w.WriteLine(d);

        return r.ReadLine();            // 301 BULK_STORED
    }
}
