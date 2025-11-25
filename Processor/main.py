import json
import logging
import os
import time
import pika
import seqlog
from influxdb_client import InfluxDBClient, Point, WritePrecision
from influxdb_client.client.write_api import SYNCHRONOUS
from datetime import datetime, timezone

RABBITMQ_HOST = os.getenv("RABBITMQ_HOST", "localhost")
RABBITMQ_INGESTOR_QUEUE = os.getenv("RABBITMQ_INGESTOR_QUEUE", "ingestor.queue")
RABBITMQ_NOTIFIER_QUEUE = os.getenv("RABBITMQ_NOTIFIER_QUEUE", "notifier.queue")
RABBITMQ_PORT = int(os.getenv("RABBITMQ_PORT", 5674))
RABBITMQ_USER = os.getenv("RABBITMQ_USER", "guest")
RABBITMQ_PASS = os.getenv("RABBITMQ_PASS", "guest")

INFLUXDB_URL = os.getenv("INFLUXDB_URL", "http://localhost:8086")
INFLUXDB_TOKEN = os.getenv("INFLUXDB_TOKEN", "supersecret-token")
INFLUXDB_ORG = os.getenv("INFLUXDB_ORG", "rasmus")
INFLUXDB_BUCKET = os.getenv("INFLUXDB_BUCKET", "weakapp_data")
SEQ_URL = os.getenv("SEQ_SERVER_URL", "http://localhost:5341") 

client = InfluxDBClient(url=INFLUXDB_URL, token=INFLUXDB_TOKEN, org=INFLUXDB_ORG)
write_api = client.write_api(write_options=SYNCHRONOUS)

MAX_RETRIES = 5
RETRY_DELAY = 5

seqlog.log_to_seq(
   server_url=SEQ_URL,
   api_key="SeqTokenForApps12345",
   level=logging.INFO,
   batch_size=10,
   auto_flush_timeout=10,
   override_root_logger=True,
   json_encoder_class=json.encoder.JSONEncoder,
   support_extra_properties=True
)

# seqlog.log_to_console(
#    level=logging.INFO,
#    support_extra_properties=True
# )

seqlog.set_global_log_properties(ApplicationType="Processor")

for attempt in range(1, MAX_RETRIES + 1):
    try:
        connection = pika.BlockingConnection(
            pika.ConnectionParameters(
                host=RABBITMQ_HOST,
                port=RABBITMQ_PORT,
                credentials=pika.PlainCredentials(RABBITMQ_USER, RABBITMQ_PASS),
                heartbeat=60
            )
        )

        channel = connection.channel()
        
        notifier_channel = connection.channel()

        notifier_channel.queue_declare(queue=RABBITMQ_NOTIFIER_QUEUE, durable=True)

        logging.info("Connected to RabbitMQ")
        
        break 
    except pika.exceptions.AMQPConnectionError as e:
        logging.warning(f"RabbitMQ connection failed (attempt {attempt}/{MAX_RETRIES}): {e}", attempt)
        if attempt == MAX_RETRIES:
            logging.error("Max retries reached. Exiting.")
            raise
        sleep_time = RETRY_DELAY * attempt 
        time.sleep(sleep_time)

def parse_value(value):
    if isinstance(value, bool):
        return 1.0 if value else 0.0
    try:
        return float(value)
    except (ValueError, TypeError):
        return str(value)

def callback(ch, method, properties, body):
    try:
        messages = json.loads(body)
        if not isinstance(messages, list):
            messages = [messages]   
        
        msg_timestamp = None
        if properties.timestamp is not None:
            msg_timestamp = datetime.fromtimestamp(properties.timestamp, tz=timezone.utc)
        
        traceId = None
        if properties.headers.get("TraceId") is not None:
            traceId = properties.headers.get("TraceId")

        seqlog.set_global_log_properties(ApplicationType="Processor", TraceId=traceId)

        logging.info(f"Received: {len(messages)} entities from Ingestor", extra={"ApplicationType": "Processor", "TraceId" : traceId}) 

        for msg in messages:
            measurement = msg.get('type')
            name_tag = msg.get('name')
            payload = msg.get('payload', {})  

            print(payload)  
            
            point = Point(measurement).tag("name", name_tag)    
            for key, value in payload.items():
                parsed_value = parse_value(value)
                point.field(key, parsed_value)

            if msg_timestamp:
                point.time(msg_timestamp, WritePrecision.NS)
            else:
                point.time(datetime.now(timezone.utc), WritePrecision.NS)

            write_api.write(bucket=INFLUXDB_BUCKET, org=INFLUXDB_ORG, record=point)

            notifier_payload = {
                "type": measurement,
                "name": name_tag,
                "payload": payload,
                "timestamp": msg_timestamp.isoformat() if msg_timestamp else datetime.now(timezone.utc).isoformat(),
                "traceId": traceId
            }

            notifier_channel.basic_publish(
                exchange='',
                routing_key=RABBITMQ_NOTIFIER_QUEUE,
                body=json.dumps(notifier_payload),
                properties=pika.BasicProperties(
                    delivery_mode=2  # make message persistent
                )
            )

            logging.info(f"Sent to notifier queue: {RABBITMQ_NOTIFIER_QUEUE}", extra={"ApplicationType": "Processor", "TraceId" : traceId})

        ch.basic_ack(delivery_tag=method.delivery_tag) 

    except Exception as e:
        logging.error(
            f"Error processing message: {e}", 
            extra={"ApplicationType": "Processor"}
        )
        ch.basic_nack(delivery_tag=method.delivery_tag, requeue=False)

print("Waiting for messages. To exit press CTRL+C")
channel.basic_consume(queue=RABBITMQ_INGESTOR_QUEUE, on_message_callback=callback)

try:
    channel.start_consuming()
except KeyboardInterrupt:
    print("Stopping consumer...")
    channel.stop_consuming()
    connection.close()
