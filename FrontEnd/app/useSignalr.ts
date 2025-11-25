import { useEffect, useState, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { Metric } from "./models/metric.model";

export function useSignalR(hubUrl: string) {
  const [message, setMessage] = useState<Metric | undefined>(undefined);
  const connectionRef = useRef<signalR.HubConnection>(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    connection
      .start()
      .then(() => console.log("✅ Connected to SignalR Hub"))
      .catch((err) => console.error("❌ SignalR Connection Error:", err));

    // Пример слушателя: метод, который вызывает сервер
    connection.on("TelemetryReceived", (sensorEvent) => {
      setMessage(sensorEvent);
    });

    return () => {
      connection.stop();
    };
  }, [hubUrl]);

  return { message };
}
