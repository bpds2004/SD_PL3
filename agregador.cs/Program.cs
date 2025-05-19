using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

class Agregador
{
    /*–– permite vários listeners a correr em paralelo ––*/
    static readonly ConcurrentDictionary<int, TcpListener> listeners = new();

    static void Main()
    {
        bool sair = false;

        while (!sair)
        {
            Console.Write("[AGREGADOR] Quantos Agregadores pretende criar? (clique 'q' para sair) ");
            string inp = Console.ReadLine();
            if (inp.Trim().Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                sair = true;
                break;
            }

            int numAgregadores = string.IsNullOrWhiteSpace(inp) ? 0 : int.Parse(inp);

            /*–– criar os listeners pedidos ––*/
            for (int i = 0; i < numAgregadores; i++)
            {
                Console.Write("[AGREGADOR]   Porta para o Agregador #{i + 1}: ");

                int porta = int.Parse(Console.ReadLine());

                if (listeners.ContainsKey(porta))
                {
                    Console.WriteLine("[AGREGADOR] Já existe listener nessa porta.");
                    continue;
                }

                var lst = new TcpListener(IPAddress.Any, porta);
                if (listeners.TryAdd(porta, lst))
                {
                    lst.Start();
                    new Thread(() => Escutar(lst, porta)).Start();
                    Console.WriteLine($"[AGREGADOR:{porta}] ativo.");
                }
            }

            Thread.Sleep(40_000);   // 40 seg (meramente para não saturar CPU)
        }

        /*–– shutdown global ––*/
        foreach (var kv in listeners) kv.Value.Stop();
    }

    /*–––– escuta contínua para cada porta ––––*/
    static void Escutar(TcpListener lst, int porta)
    {
        while (true)
        {
            TcpClient cli;
            try { cli = lst.AcceptTcpClient(); }
            catch { break; }   // listener foi fechado
            new Thread(() => TratarWavy(cli, porta)).Start();
        }
    }

    /*–––– trata cada ligação de WAVY ––––*/
    static void TratarWavy(TcpClient wavy, int porta)
    {
        var r = new StreamReader(wavy.GetStream());
        var w = new StreamWriter(wavy.GetStream()) { AutoFlush = true };
        string id = "";

        try
        {
            /*–– FASE 2 : HELLO ––*/
            string hello = r.ReadLine();
            Console.WriteLine($"[AGREGADOR:{porta}] Recebido da WAVY: {hello}");

            if (hello.StartsWith("HELLO"))
            {
                id = hello.Split(' ')[1];
                w.WriteLine("100 OK");

                Console.WriteLine($"[AGREGADOR:{porta}] A registar ID '{id}' no SERVIDOR…");
                Console.WriteLine($"[AGREGADOR:{porta}] Resposta do SERVIDOR: {EnviarServidor($"REGISTER {id}")}");
            }

            /*–– ciclo de trabalho ––*/
            while (true)
            {
                string header = r.ReadLine();          // DATA_BULK ou QUIT
                if (header == null) break;

                /*–– FASE 3 : Dados ––*/
                if (header.StartsWith("DATA_BULK"))
                {
                    Console.WriteLine($"[AGREGADOR:{porta}] Cabeçalho recebido: {header}");

                    string[] h = header.Split(' ');
                    string wavyId = h[1];
                    int n = int.Parse(h[2]);

                    string[] dados = new string[n];
                    for (int i = 0; i < n; i++)
                    {
                        dados[i] = r.ReadLine();
                        Console.WriteLine($"[AGREGADOR:{porta}] Dado recebido: {dados[i]}");
                    }

                    Console.WriteLine($"[AGREGADOR:{porta}] A reenviar dados ao SERVIDOR…");
                    string resp = EnviarServidorBulk(wavyId, porta, dados);
                    Console.WriteLine($"[AGREGADOR:{porta}] SERVIDOR respondeu: {resp}");
                    w.WriteLine(resp);                 // 301 BULK_STORED
                    continue;                          // volta a aguardar próximo header
                }

                /*–– FASE 4 : QUIT ––*/
                if (header == "QUIT")
                {
                    Console.WriteLine($"[AGREGADOR:{porta}] WAVY diz: QUIT");
                    Console.WriteLine($"[AGREGADOR:{porta}] A informar desconexão do ID {id} ao SERVIDOR…");
                    Console.WriteLine($"[AGREGADOR:{porta}] SERVIDOR respondeu: {EnviarServidor($"DISCONNECT {id}")}");
                    w.WriteLine("QUIT_OK");
                    Console.WriteLine($"[AGREGADOR:{porta}] {id} desconectado.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AGREGADOR:{porta}] Erro: {ex.Message}");
        }
        finally
        {
            wavy.Close();
        }
    }

    /*–– request simples ––*/
    static string EnviarServidor(string msg)
    {
        using var srv = new TcpClient("127.0.0.1", 6000);
        var rr = new StreamReader(srv.GetStream());
        var ww = new StreamWriter(srv.GetStream()) { AutoFlush = true };
        ww.WriteLine(msg);
        return rr.ReadLine();
    }

    /*–– === FASE 3 : Bulk ––*/
    static string EnviarServidorBulk(string id, int porta, string[] dados)
    {
        using var s = new TcpClient("127.0.0.1", 6000);
        var rr = new StreamReader(s.GetStream());
        var ww = new StreamWriter(s.GetStream()) { AutoFlush = true };

        ww.WriteLine($"FORWARD_BULK {id} {porta} {dados.Length}");
        foreach (var d in dados) ww.WriteLine(d);

        return rr.ReadLine();   // 301 BULK_STORED
    }
}
