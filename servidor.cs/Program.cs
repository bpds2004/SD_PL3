// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

// FASE 3/SERVIDOR/Program.cs
/*using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    static void Main()
    {
        int port = 6000;
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"SERVIDOR a escutar na porta {port}...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("AGREGADOR conectado.");

            using NetworkStream stream = client.GetStream();

            string message = ReadMessage(stream);
            Console.WriteLine("<< Recebido: " + message);

            if (message.StartsWith("FORWARD_BULK"))
            {
                // Simula o armazenamento
                File.AppendAllText("dados_recebidos.txt", message + "\n");
                SendMessage(stream, "301 BULK_STORED");
            }
            else if (message == "QUIT")
            {
                SendMessage(stream, "400 BYE");
                Console.WriteLine("Desligar conexão.");
            }
        }
    }

    static string ReadMessage(NetworkStream stream)
    {
        byte[] buffer = new byte[2048];
        int length = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, length).Trim();
    }

    static void SendMessage(NetworkStream stream, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        stream.Write(data, 0, data.Length);
    }
}
FASE2
*/
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ClosedXML.Excel; // ===================== FASE 5: Biblioteca para manipular ficheiros Excel =====================

class Servidor
{
    // ===================== FASE 4: Mutex para acesso sequencial ao ficheiro =====================
    static readonly object lockFicheiro = new object();

    static void Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 6000);
        listener.Start();
        Console.WriteLine("[SERVIDOR] A ouvir na porta 6000...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();

            // ===================== FASE 4: Atendimento concorrente com Threads =====================
            Thread t = new Thread(() => TratarCliente(client));
            t.Start();
        }
    }

    static void TratarCliente(TcpClient client)
    {
        Console.WriteLine("[SERVIDOR] Ligado ao AGREGADOR.");
        StreamReader reader = new StreamReader(client.GetStream());
        StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

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

        // ===================== FASE 3: Receber dados do AGREGADOR =====================
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

            // ===================== FASE 4: Escrita protegida por mutex =====================
            lock (lockFicheiro)
            {
                // ===================== FASE 4: Guardar dados num ficheiro de texto =====================
                string ficheiroTxt = $"dados_{id}.txt";
                using (StreamWriter sw = new StreamWriter(ficheiroTxt, append: true))
                {
                    foreach (string linha in dados)
                    {
                        sw.WriteLine(linha);
                    }
                }

                // ===================== FASE 5: Guardar dados num ficheiro Excel =====================
                string ficheiroExcel = "dadosTP1.xlsx";

                XLWorkbook workbook;
                IXLWorksheet worksheet;

                // Verifica se o ficheiro Excel já existe
                if (File.Exists(ficheiroExcel))
                {
                    workbook = new XLWorkbook(ficheiroExcel);
                    worksheet = workbook.Worksheet(1);
                }
                else
                {
                    workbook = new XLWorkbook();
                    worksheet = workbook.Worksheets.Add("Registos");

                    // Cabeçalhos na primeira linha
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Dado";
                    worksheet.Cell(1, 3).Value = "DataHora";
                }

                // Determina a próxima linha disponível
                int ultimaLinha = worksheet.LastRowUsed()?.RowNumber() ?? 1;

                // Escreve os dados no Excel
                foreach (string linha in dados)
                {
                    ultimaLinha++;
                    worksheet.Cell(ultimaLinha, 1).Value = id;
                    worksheet.Cell(ultimaLinha, 2).Value = linha;
                    worksheet.Cell(ultimaLinha, 3).Value = DateTime.Now;
                }

                // Guarda o ficheiro Excel
                workbook.SaveAs(ficheiroExcel);
            }

            writer.WriteLine("301 BULK_STORED");
        }
        // ===================== FIM DA FASE 3 =====================

        client.Close();
    }
}
