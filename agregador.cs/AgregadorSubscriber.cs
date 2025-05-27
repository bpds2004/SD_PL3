using System;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DocumentFormat.OpenXml.EMMA;

class AgregadorSubscriber
{
    static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };

        using var connection = factory.CreateConnection(new string[] { "localhost" });
        using var channel = connection.CreateModel();

        string exchangeName = "sensor_data";
        channel.ExchangeDeclare(exchange: exchangeName, type: "topic");

        // Cria fila temporária exclusiva para este consumidor
        string queueName = channel.QueueDeclare().QueueName;

        // Subscrição dos tópicos desejados
        channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "sensor.temperatura");
        channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "sensor.humidade");

        Console.WriteLine("[Agregador] Aguardando mensagens...");

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var mensagem = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            Console.WriteLine($"[Agregador] Recebeu '{mensagem}' do tópico '{routingKey}'");

            // Exemplo de chamada RPC para pré-processamento
            string resposta = ChamadaRPC(channel, mensagem);
            Console.WriteLine($"[Agregador] Resposta do serviço de pré-processamento: {resposta}");
        };

        channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        Console.WriteLine("Pressione Enter para sair.");
        Console.ReadLine();
    }

    // Função de chamada RPC usando RabbitMQ
    static string ChamadaRPC(IModel channel, string mensagem)
    {
        var correlationId = Guid.NewGuid().ToString();
        var replyQueue = channel.QueueDeclare().QueueName;
        var props = channel.CreateBasicProperties();
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueue;

        var body = Encoding.UTF8.GetBytes(mensagem);
        channel.BasicPublish(exchange: "", routingKey: "rpc_preprocessamento", basicProperties: props, body: body);

        var resposta = "";
        var respConsumer = new EventingBasicConsumer(channel);
        var resetEvent = new AutoResetEvent(false);

        respConsumer.Received += (model, ea) =>
        {
            if (ea.BasicProperties.CorrelationId == correlationId)
            {
                resposta = Encoding.UTF8.GetString(ea.Body.ToArray());
                resetEvent.Set();
            }
        };

        channel.BasicConsume(queue: replyQueue, autoAck: true, consumer: respConsumer);

        // Aguarda resposta (timeout de 5 segundos)
        resetEvent.WaitOne(5000);
        return resposta;
    }
}
