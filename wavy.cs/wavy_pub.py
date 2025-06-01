# wavy/wavy_pub.py
import pika
import json
import time
import random

connection = pika.BlockingConnection(pika.ConnectionParameters('localhost'))
channel = connection.channel()

sensor_type = 'temperatura'  # tópico
queue_name = f'sensor.{sensor_type}'

# Declarar exchange tipo topic
channel.exchange_declare(exchange='sensores', exchange_type='topic')

while True:
    dado = {
        "id": "WAVY_01",
        "tipo": sensor_type,
        "valor": round(random.uniform(10.0, 25.0), 2),
        "timestamp": time.time()
    }
    body = json.dumps(dado)
    channel.basic_publish(
        exchange='sensores',
        routing_key=queue_name,
        body=body
    )
    print(f"[WAVY] Publicado: {body}")
    time.sleep(2)
