import { connect } from "amqplib";
import { HubConnectionBuilder } from "@microsoft/signalr";
import dotenv from "dotenv";

dotenv.config();

const hubConnection = new HubConnectionBuilder()
  .withUrl(process.env.SIGNALR_URL)
  .withAutomaticReconnect()
  .build();

async function startSignalR() {
  await hubConnection.start();
  console.log("✅ Connected to SignalR Hub");
}

async function listenQueue() {
  const connection = await connect(process.env.RABBITMQ_URL);
  const channel = await connection.createChannel();
  const queue = "notifier.queue";

  await channel.assertQueue(queue, { durable: true });
  console.log(`✅ Listening to RabbitMQ queue: ${queue}`);

  channel.consume(queue, async (msg) => {
    if (msg !== null) {
      const eventData = JSON.parse(msg.content.toString());
      console.log(`📩 Received event: ${eventData}`);

      try {
        const sensorEvent = {
          Type: eventData.type ?? "",
          Room: eventData.name ?? "",
          Time: eventData.timestamp
            ? new Date(eventData.timestamp)
            : new Date(),
          Energy: eventData.payload?.energy ?? null,
          Co2: eventData.payload?.co2 ?? null,
          Pm25: eventData.payload?.pm25 ?? null,
          Humidity: eventData.payload?.humidity ?? null,
          MotionDetected: eventData.payload?.motionDetected ?? null,
        };

        await hubConnection.invoke("TelemetryReceived", sensorEvent);
        console.log("➡️ Event sent to SignalR Hub");
      } catch (err) {
        console.error("❌ Error sending to SignalR Hub:", err);
      }

      channel.ack(msg);
    }
  });
}

try {
  await startSignalR();
  await listenQueue();
} catch (err) {
  console.error(err);
  process.exit(1);
}
