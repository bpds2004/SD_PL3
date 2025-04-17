using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Agregador
{
    static void Main()
    {
        Console.Write("[AGREGADOR] Quantos Agregadores deseja iniciar? ");
        int numAgregadores = int.Parse(Console.ReadLine());

        int[] portas = new int[numAgregadores];

        // Primeiro, recolhe todas as portas
        for (int i = 0; i < numAgregadores; i++)
        {
            Console.Write($"[AGREGADOR] Porta para o Agregador #{i + 1}: ");
            portas[i] = int.Parse(Console.ReadLine());
        }

        // Depois, inicia cada Agregador
        foreach (int porta in portas)
        {
            new Thread(() => IniciarAgregador(porta)).Start();
        }

        Console.WriteLine("\n[AGREGADOR] Todos os Agregadores estão à escuta! Pressiona Enter para sair...");
        Console.ReadLine();
    }

    static void IniciarAgregador(int porta)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, porta);
        listener.Start();
        Console.WriteLine($"[AGREGADOR:{porta}] À espera de WAVYs na porta {porta}...");

        while (true)
        {
            TcpClient wavy = listener.AcceptTcpClient();
            new Thread(() => TratarWavy(wavy, porta)).Start();
        }
    }

    static void TratarWavy(TcpClient wavy, int porta)
    {
        Console.WriteLine($"[AGREGADOR:{porta}] WAVY conectada.");
        StreamReader wavyReader = new StreamReader(wavy.GetStream());
        StreamWriter wavyWriter = new StreamWriter(wavy.GetStream()) { AutoFlush = true };

        string id = "";

        try
        {
            // === FASE 2: HELLO ===
            string hello = wavyReader.ReadLine();
            Console.WriteLine($"[AGREGADOR:{porta}] Recebido da WAVY: {hello}");

            if (hello.StartsWith("HELLO"))
            {
                id = hello.Split(' ')[1];
                wavyWriter.WriteLine("100 OK");

                TcpClient servidor = new TcpClient("127.0.0.1", 6000);
                StreamReader srvReader = new StreamReader(servidor.GetStream());
                StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

                Console.WriteLine($"[AGREGADOR:{porta}] A registar ID '{id}' no SERVIDOR...");
                srvWriter.WriteLine("REGISTER " + id);
                string regResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR:{porta}] Resposta do SERVIDOR: {regResp}");

                servidor.Close();
            }

            // === FASE 3: Dados ===
            string dataBulkHeader = wavyReader.ReadLine();
            Console.WriteLine($"[AGREGADOR:{porta}] Cabeçalho recebido: {dataBulkHeader}");
            if (dataBulkHeader.StartsWith("DATA_BULK"))
            {
                string[] headerParts = dataBulkHeader.Split(' ');
                string wavyId = headerParts[1];
                int n_dados = int.Parse(headerParts[2]);

                string[] dados = new string[n_dados];
                for (int i = 0; i < n_dados; i++)
                {
                    dados[i] = wavyReader.ReadLine();
                    Console.WriteLine($"[AGREGADOR:{porta}] Dado recebido: {dados[i]}");
                }

                TcpClient servidor = new TcpClient("127.0.0.1", 6000);
                StreamReader srvReader = new StreamReader(servidor.GetStream());
                StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

                Console.WriteLine($"[AGREGADOR:{porta}] A reenviar dados ao SERVIDOR...");
                srvWriter.WriteLine($"FORWARD_BULK {wavyId} {n_dados}");
                foreach (string dado in dados)
                {
                    srvWriter.WriteLine(dado);
                }

                string bulkResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR:{porta}] SERVIDOR respondeu: {bulkResp}");

                servidor.Close();
            }

            // === FASE 4: QUIT ===
            string quit = wavyReader.ReadLine();
            Console.WriteLine($"[AGREGADOR:{porta}] WAVY diz: {quit}");

            if (quit == "QUIT")
            {
                TcpClient servidor = new TcpClient("127.0.0.1", 6000);
                StreamReader srvReader = new StreamReader(servidor.GetStream());
                StreamWriter srvWriter = new StreamWriter(servidor.GetStream()) { AutoFlush = true };

                Console.WriteLine($"[AGREGADOR:{porta}] A informar desconexão do ID {id} ao SERVIDOR...");
                srvWriter.WriteLine("DISCONNECT " + id);
                string byeResp = srvReader.ReadLine();
                Console.WriteLine($"[AGREGADOR:{porta}] SERVIDOR respondeu: {byeResp}");

                servidor.Close();
            }

            wavyWriter.WriteLine("QUIT_OK");
            Console.WriteLine($"[AGREGADOR:{porta}] {id} desconectado.");
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
}
