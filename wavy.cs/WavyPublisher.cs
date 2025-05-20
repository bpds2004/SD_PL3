using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


class WavyPublisher
{
    static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost", Port = 5672 };

        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            string exchangeName = "sensor_data";
            channel.ExchangeDeclare(exchange: exchangeName, type: "topic");

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
}

