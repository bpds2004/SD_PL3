# aggregator/aggregator_sub.py
import pika
import json

def callback(ch, method, properties, body):
    dado = json.loads(body)
    print(f"[AGREGADOR] Recebido: {dado}")
    # Aqui seria onde se chamaria o RPC de pré-processamento futuramente

connection = pika.BlockingConnection(pika.ConnectionParameters('localhost'))
channel = connection.channel()

channel.exchange_declare(exchange='sensores', exchange_type='topic')

# Subscrição do Agregador a 'sensor.temperatura'
queue_result = channel.queue_declare('', exclusive=True)
queue_name = queue_result.method.queue
channel.queue_bind(exchange='sensores', queue=queue_name, routing_key='sensor.temperatura')

print('[AGREGADOR] À espera de mensagens...')
channel.basic_consume(queue=queue_name, on_message_callback=callback, auto_ack=True)
channel.start_consuming()
