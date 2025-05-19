using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

class AgregadorSubscriber
{
    static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        string exchangeName = "sensor_data";
        channel.ExchangeDeclare(exchange: exchangeName, type: "topic");

        // Cria fila temporária exclusiva para este consumidor
        string queueName = channel.QueueDeclare().QueueName;

        // Subscrição dos tópicos desejados
        // Exemplo: recebe temperatura e humidade
        channel.QueueBind(queue: queueName,
                          exchange: exchangeName,
                          routingKey: "sensor.temperatura");

        channel.QueueBind(queue: queueName,
                          exchange: exchangeName,
                          routingKey: "sensor.humidade");

        Console.WriteLine("[Agregador] Aguardando mensagens...");

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var mensagem = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            Console.WriteLine($"[Agregador] Recebeu '{mensagem}' do tópico '{routingKey}'");
        };

        channel.BasicConsume(queue: queueName,
                             autoAck: true,
                             consumer: consumer);

        Console.WriteLine("Pressione Enter para sair.");
        Console.ReadLine();
    }
}

