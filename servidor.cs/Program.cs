using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Servidor
{
    // === NOVO === cabeçalho desejado
    const string ficheiro = "dados_servidor.csv";
    static readonly object lockCsv = new object();

    static void Main()
    {
        /*–– cria CSV c/ cabeçalho se não existir ––*/
        if (!File.Exists(ficheiro))
            File.WriteAllText(ficheiro,
                "ID,Porta,Temperatura,Humidade,Pressao,DataHora\n");

        TcpListener lst = new TcpListener(IPAddress.Any, 6000);
        lst.Start();
        Console.WriteLine("[SERVIDOR] A ouvir na porta 6000...");

        while (true)
            new Thread(() => Tratar(lst.AcceptTcpClient())).Start();
    }

    /*–– thread p/ cada ligação ––*/
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
            //  FORWARD_BULK  id  porta  n
            string[] p = linha.Split(' ');
            string id = p[1];
            string porta = p[2];
            int n = int.Parse(p[3]);

            string[] leituras = new string[n];
            for (int i = 0; i < n; i++) leituras[i] = r.ReadLine();

            string temp = Valor("temperatura", leituras);
            string hum = Valor("humidade", leituras);
            string pres = Valor("pressao", leituras);

            lock (lockCsv)
            {
                string csv = $"{id},{porta},{temp},{hum},{pres}," +
                             $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                File.AppendAllText(ficheiro, csv);
            }
            w.WriteLine("301 BULK_STORED");
        }


        cli.Close();
    }

    /*–– utilitário local ––*/
    static string Valor(string chave, string[] arr)
    {
        foreach (var l in arr)
            if (l.StartsWith(chave)) return l.Split(':')[1];
        return "";
    }
}
