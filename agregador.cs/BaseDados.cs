using System;
using System.Data.SQLite;

public static class BaseDados
{
    private const string FICHEIRO_DB = "dados.db";

    public static void Inicializar()
    {
        if (!File.Exists(FICHEIRO_DB))
        {
            SQLiteConnection.CreateFile(FICHEIRO_DB);
            using var conn = new SQLiteConnection($"Data Source={FICHEIRO_DB}");
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE dados (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    id_wavy TEXT,
                    sensor TEXT,
                    valor REAL,
                    timestamp INTEGER
                );

                CREATE TABLE analises (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    sensor TEXT,
                    media REAL,
                    total INTEGER,
                    timestamp INTEGER
                );
            ";
            cmd.ExecuteNonQuery();
        }
    }

    public static void GuardarDado(string idWavy, string sensor, double valor, long timestamp)
    {
        using var conn = new SQLiteConnection($"Data Source={FICHEIRO_DB}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO dados (id_wavy, sensor, valor, timestamp) VALUES (@id, @sensor, @valor, @ts)";
        cmd.Parameters.AddWithValue("@id", idWavy);
        cmd.Parameters.AddWithValue("@sensor", sensor);
        cmd.Parameters.AddWithValue("@valor", valor);
        cmd.Parameters.AddWithValue("@ts", timestamp);
        cmd.ExecuteNonQuery();
    }

    public static void GuardarAnalise(string sensor, double media, int total)
    {
        using var conn = new SQLiteConnection($"Data Source={FICHEIRO_DB}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO analises (sensor, media, total, timestamp) VALUES (@sensor, @media, @total, @ts)";
        cmd.Parameters.AddWithValue("@sensor", sensor);
        cmd.Parameters.AddWithValue("@media", media);
        cmd.Parameters.AddWithValue("@total", total);
        cmd.Parameters.AddWithValue("@ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        cmd.ExecuteNonQuery();
    }
}
