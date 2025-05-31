using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class WavyPublisher
{
    static void Main()
    {
        // Configuração da conexão
        var factory = new ConnectionFactory() { HostName = "localhost" };

        // Corrigido: Usar CreateConnectionAsync para criar a conexão
        using var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        using var channel = connection.CreateModel();

        // Declara o exchange do tipo 'topic' para Pub/Sub
        string exchangeName = "sensor_data";
        channel.ExchangeDeclare(exchange: exchangeName, type: "topic");

        // Dados de sensores para simular publicação
        var dados = new[]
        {
            ("sensor.temperatura", "22.5"),
            ("sensor.humidade", "45"),
            ("sensor.pressao", "1012")
        };

        foreach (var (routingKey, mensagem) in dados)
        {
            var body = Encoding.UTF8.GetBytes(mensagem);

            channel.BasicPublish(exchange: exchangeName,
                                 routingKey: routingKey,
                                 basicProperties: null,
                                 body: body);

            Console.WriteLine($"[WAVY] Publicou '{mensagem}' em tópico '{routingKey}'");
        }

        Console.WriteLine("Publicação concluída. Pressione Enter para sair.");
        Console.ReadLine();
    }
}
