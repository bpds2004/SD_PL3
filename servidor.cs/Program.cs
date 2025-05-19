/*
 * SERVIDOR
 * --------
 * – Ouve na porta 6000.
 * – REGISTER  → devolve 200 REGISTERED
 * – DISCONNECT → devolve 400 BYE
 * – FORWARD_BULK id porta n + n linhas  → grava/append no CSV e devolve 301 BULK_STORED
 * – Protege escrita concorrente ao ficheiro com lock.
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Servidor
{
    const string CSV = "dados_servidor.csv";
    static readonly object lockCsv = new();

    static void Main()
    {
        /* Se não existir, cria o CSV com cabeçalho */
        if (!File.Exists(CSV))
            File.WriteAllText(CSV, "ID,Porta,Temperatura,Humidade,Pressao,DataHora\n");

        var listener = new TcpListener(IPAddress.Any, 6000);
        listener.Start();
        Console.WriteLine("[SERVIDOR] A ouvir na porta 6000...");

        while (true)
            new Thread(() => Tratar(listener.AcceptTcpClient())).Start();
    }

    /* ========================================================= */

    static void Tratar(TcpClient cli)
    {
        var r = new StreamReader(cli.GetStream());
        var w = new StreamWriter(cli.GetStream()) { AutoFlush = true };

        string linha = r.ReadLine();
        Console.WriteLine($"[SERVIDOR] Recebido: {linha}");

        if (linha.StartsWith("REGISTER"))
        {
            w.WriteLine("200 REGISTERED");
        }
        else if (linha.StartsWith("DISCONNECT"))
        {
            w.WriteLine("400 BYE");
        }
        else if (linha.StartsWith("FORWARD_BULK"))
        {
            /*   FORWARD_BULK id porta n   */
            string[] p = linha.Split(' ');
            string id = p[1];
            string porta = p[2];
            int n = int.Parse(p[3]);

            string[] leituras = new string[n];
            for (int i = 0; i < n; i++) leituras[i] = r.ReadLine();

            /* Extrai valores */
            string temp = Valor("temperatura", leituras);
            string hum = Valor("humidade", leituras);
            string pres = Valor("pressao", leituras);

            /* Escreve CSV de forma thread‑safe */
            lock (lockCsv)
            {
                File.AppendAllText(CSV,
                    $"{id},{porta},{temp},{hum},{pres},{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
            }
            w.WriteLine("301 BULK_STORED");
        }

        cli.Close();
    }

    static string Valor(string chave, string[] arr)
    {
        foreach (string l in arr)
            if (l.StartsWith(chave + ":"))
                return l.Split(':')[1];
        return "";
    }
}
