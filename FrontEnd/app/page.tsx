"use client";

import { useState, useEffect, useCallback } from "react";
import { apolloServerClient } from "@/services/apollo";
import { GET_METRICS } from "@/services/graph-ql/queries/get-metrics";
import {
  Chart as ChartJS,
  ArcElement,
  Tooltip,
  Legend,
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  RadialLinearScale,
  Filler,
} from "chart.js";
import { MetricsTable } from "./components/metrics-table.component";
import { useSignalR } from "./useSignalr";
import { AirQualityCharts } from "./components/analytics/air-quality-charts.component";
import { MotionCharts } from "./components/analytics/motion-charts.component";
import { EnergyCharts } from "./components/analytics/energy-charts.component";
import { FloorPlan } from "./components/floor-plan.component";

// Регистрируем необходимые компоненты Chart.js
ChartJS.register(
  ArcElement,
  Tooltip,
  Legend,
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  RadialLinearScale,
  Filler
);

type Metric = {
  type: string;
  room: string;
  time: string;
  energy?: number;
  motionDetected?: boolean;
  co2?: number;
  pm25?: number;
  humidity?: number;
};

export default function MetricsPage() {
  const [, setMetrics] = useState<Metric[]>([]);

  const [enableRealtimeUpdates, setRealtimeUpdates] = useState(true);

  const [realtimeMetrics, setRealtimeMetrics] = useState<Metric[]>([]);

  const [isLatestEnergyMeticReceived, setIsLatestEnergyMeticReceived] =
    useState<boolean>(false);

  const [isLatestMotionMetricReceived, setIsLatestMotionMetricReceived] =
    useState<boolean>(false);

  const [
    isLatestAirQualityMetricReceived,
    setIsLatestAirQualityMetricReceived,
  ] = useState<boolean>(false);

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // -- Paging
  // const [skip] = useState<number>(0);
  // const [take] = useState<number>(15);

  // --- загруженные с бэка lists ---
  const [rooms, setRooms] = useState<string[]>([]);
  const [types, setTypes] = useState<string[]>([]);

  // --- выбранные фильтры ---
  const [room, setRoom] = useState("all");
  const [typeFilter, setTypeFilter] = useState("all");

  const [from, setFrom] = useState<Date>(
    new Date(Date.now() - 30 * 24 * 60 * 60 * 1000)
  );
  const [to, setTo] = useState(new Date());

  // --- выбор типа метрики для детальных графиков ---
  const [metricType, setMetricType] = useState<
    "overview" | "energy" | "motion" | "air_quality"
  >("overview");

  // --- основная навигация по разделам ---
  const [mainView, setMainView] = useState<"table" | "analytics" | "floorplan">(
    "table"
  );

  const signalRUrl =
    process.env.NEXT_PUBLIC_SIGNALR_URL || "http://localhost:5148/assessment";

  const { message } = useSignalR(signalRUrl);

  useEffect(() => {
    if (message) {
      if (message?.energy != undefined) {
        setIsLatestEnergyMeticReceived(true);
      }

      if (message.motionDetected != undefined) {
        setIsLatestMotionMetricReceived(true);
      }

      if (message.co2 || message.pm25 || message.humidity) {
        setIsLatestAirQualityMetricReceived(true);
      }

      setRealtimeMetrics((prev) => [message, ...prev]);
    }
  }, [message]);

  const handleLockToggle = () => {
    setRealtimeUpdates((prev) => !prev);

    if (!enableRealtimeUpdates) {
      // Включили lock → очищаем таблицу
      setRoom("all");
      setTypeFilter("all");
      setMetrics([]);
    } else {
      setRealtimeMetrics([]);
      // // Выключили lock → грузим заново по текущим фильтрам
      // fetchMetrics(room, typeFilter, from, to);
    }
  };

  // ----------------------------
  //  ЗАГРУЖАЕМ rooms + types
  // ----------------------------
  useEffect(() => {
    console.warn(process.env.NEXT_PUBLIC_REST_API_URL);
    console.warn(process.env.NEXT_PUBLIC_GRAPHQL_URL);

    async function loadMetadata() {
      try {
        const restApiUrl =
          process.env.NEXT_PUBLIC_REST_API_URL || "http://localhost:5148";
        const res = await fetch(`${restApiUrl}/metrics/metadata`);
        if (!res.ok) {
          throw new Error("Failed to load metadata");
        }
        const data = await res.json();

        setRooms([...data.rooms, "all"]);
        setTypes(data.types || []);

        // дефолтные значения
        if (!data.rooms?.length) {
          setRoom("all");
        }
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Failed to load metadata"
        );
      }
    }

    loadMetadata();
  }, []);

  // ----------------------------
  //  ЗАПРОС МЕТРИК
  // ----------------------------
  const fetchMetrics = useCallback(
    async (
      room?: string,
      type?: string,
      from?: string,
      to?: string,
      skip?: number,
      take?: number
    ) => {
      if (isLoading) {
        return;
      }

      setIsLoading(true);
      setError(null);

      try {
        const { data } = await apolloServerClient.query<{ metrics: Metric[] }>({
          query: GET_METRICS,
          variables: {
            room: room !== "all" ? room : null,
            type: type !== "all" ? type : null,
            from,
            to,
            skip,
            take,
          },
          fetchPolicy: "no-cache",
        });

        const allMetricsData = data?.metrics ?? [];

        // // Сохраняем все метрики для графика
        // setAllMetrics(allMetricsData);

        // Применяем фильтр по типу на клиенте для таблицы
        const filteredMetrics =
          typeFilter === "all"
            ? allMetricsData
            : allMetricsData.filter((m) => m.type === typeFilter);

        setMetrics(filteredMetrics);

        return filteredMetrics;
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load metrics");
        setMetrics([]);
      } finally {
        setIsLoading(false);
      }
    },
    [typeFilter, isLoading]
  );

  return (
    <main className="h-screen bg-gray-50 flex flex-col overflow-hidden">
      {/* Верхняя навигация */}
      <div className="bg-white border-b shadow-sm sticky top-0 z-20">
        <div className="px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex gap-4">
              <h1 className="text-2xl font-bold">Smarthome Metrics 🐗</h1>
              <select
                className="px-3 py-2 border rounded-lg"
                value={room}
                disabled={enableRealtimeUpdates}
                onChange={(e) => setRoom(e.target.value)}
              >
                <option value="all">All rooms</option>
                {rooms.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>

              <select
                className="px-3 py-2 border rounded-lg"
                value={typeFilter}
                disabled={enableRealtimeUpdates}
                onChange={(e) => setTypeFilter(e.target.value)}
              >
                <option value="all">All sensor types</option>
                {types.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>

              {/* Период — From */}
              <input
                type="datetime-local"
                className="px-3 py-2 border rounded-lg"
                value={formatDate(from)}
                max={formatDate(to)}
                disabled={
                  enableRealtimeUpdates ||
                  metricType === "motion" ||
                  metricType === "air_quality"
                }
                onChange={(e) => setFrom(new Date(e.target.value))}
              />

              {/* Период — To */}
              <input
                type="datetime-local"
                className="px-3 py-2 border rounded-lg"
                value={formatDate(to)}
                disabled={enableRealtimeUpdates}
                onChange={(e) => setTo(new Date(e.target.value))}
              />
              <div className="flex items-center gap-2">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={enableRealtimeUpdates}
                    onChange={handleLockToggle}
                    className="w-5 h-5"
                  />
                  <span>⚡ Enable realtime updates</span>
                </label>
              </div>
            </div>
            <div className="flex gap-2">
              <button
                onClick={() => setMainView("table")}
                className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                  mainView === "table"
                    ? "bg-blue-600 text-white"
                    : "bg-gray-100 text-gray-700 hover:bg-gray-200"
                }`}
              >
                📊 Table
              </button>
              <button
                onClick={() => setMainView("analytics")}
                className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                  mainView === "analytics"
                    ? "bg-blue-600 text-white"
                    : "bg-gray-100 text-gray-700 hover:bg-gray-200"
                }`}
              >
                📈 Analytics
              </button>
              <button
                onClick={() => setMainView("floorplan")}
                className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                  mainView === "floorplan"
                    ? "bg-blue-600 text-white"
                    : "bg-gray-100 text-gray-700 hover:bg-gray-200"
                }`}
              >
                🏠 Home Layout
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Основной контент */}
      <div className="flex-1 overflow-hidden">
        <div
          className="max-w-7xl mx-auto px-6 py-6"
          style={{ maxHeight: "100%", display: "flex" }}
        >
          {error && (
            <div className="p-4 bg-red-100 border border-red-400 text-red-700 rounded mb-4">
              {error}
            </div>
          )}

          {mainView === "table" && (
            <MetricsTable
              enableRealtimeUpdates={enableRealtimeUpdates}
              realtimeMetrics={realtimeMetrics}
              fetchMetrics={fetchMetrics}
              type={typeFilter}
              room={room}
              from={from}
              to={to}
              take={50}
            />
          )}

          {mainView === "analytics" && (
            <div className="space-y-6 w-full">
              <div
                className="bg-white rounded-lg border shadow-sm p-6"
                style={{ width: "100%" }}
              >
                <h2 className="text-2xl font-bold mb-6">Detailed Analytics</h2>
                {/* Табы для выбора типа метрики */}
                <div className="flex gap-2 border-b mb-6">
                  <button
                    onClick={() => setMetricType("energy")}
                    className={`px-6 py-3 text-sm font-medium transition-colors rounded-t-lg ${
                      metricType === "energy"
                        ? "border-b-2 border-blue-600 text-blue-600 bg-blue-50"
                        : "text-gray-600 hover:text-gray-900 hover:bg-gray-50"
                    }`}
                  >
                    ⚡ Power consumption
                  </button>
                  <button
                    onClick={() => setMetricType("motion")}
                    className={`px-6 py-3 text-sm font-medium transition-colors rounded-t-lg ${
                      metricType === "motion"
                        ? "border-b-2 border-blue-600 text-blue-600 bg-blue-50"
                        : "text-gray-600 hover:text-gray-900 hover:bg-gray-50"
                    }`}
                  >
                    🚶 Motion Detection
                  </button>
                  <button
                    onClick={() => setMetricType("air_quality")}
                    className={`px-6 py-3 text-sm font-medium transition-colors rounded-t-lg ${
                      metricType === "air_quality"
                        ? "border-b-2 border-blue-600 text-blue-600 bg-blue-50"
                        : "text-gray-600 hover:text-gray-900 hover:bg-gray-50"
                    }`}
                  >
                    🌫️ Air Quality
                  </button>
                </div>

                {/* Контент в зависимости от выбранного типа */}
                <div>
                  {metricType === "overview" && (
                    <div className="bg-gray-50 p-8 rounded-lg text-center">
                      <h3 className="text-xl font-bold mb-4">
                        Metrics Overview
                      </h3>
                      <p className="text-gray-600 mb-6">
                        Select a metric type to view detailed analytics
                      </p>
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
                        <div
                          style={{ cursor: "pointer" }}
                          className="bg-white p-4 rounded-lg border"
                          onClick={() => setMetricType("energy")}
                        >
                          <div className="text-3xl mb-2">⚡</div>
                          <h4 className="font-semibold">Power consumption</h4>
                          <p className="text-sm text-gray-500 mt-2">
                            Energy consumption analysis by room
                          </p>
                        </div>
                        <div
                          style={{ cursor: "pointer" }}
                          className="bg-white p-4 rounded-lg border"
                          onClick={() => setMetricType("motion")}
                        >
                          <div className="text-3xl mb-2">🚶</div>
                          <h4 className="font-semibold">Motion Detection</h4>
                          <p className="text-sm text-gray-500 mt-2">
                            Activity and motion detection in the house
                          </p>
                        </div>
                        <div
                          style={{ cursor: "pointer" }}
                          className="bg-white p-4 rounded-lg border"
                          onClick={() => setMetricType("air_quality")}
                        >
                          <div className="text-3xl mb-2">🌫️</div>
                          <h4 className="font-semibold">Air Quality</h4>
                          <p className="text-sm text-gray-500 mt-2">
                            CO₂, PM2.5, humidity levels
                          </p>
                        </div>
                      </div>
                    </div>
                  )}
                  {metricType === "energy" && (
                    <EnergyCharts
                      setIsLatestEnergyMeticReceived={
                        setIsLatestEnergyMeticReceived
                      }
                      isLatestEnergyMeticReceived={isLatestEnergyMeticReceived}
                      room={room}
                      isListeningForRealtimeMetrics={enableRealtimeUpdates}
                      fetchMetrics={fetchMetrics}
                      from={from}
                      to={to}
                    />
                  )}
                  {metricType === "motion" && (
                    <MotionCharts
                      timestamp={to}
                      setIsLatestMotionMeticReceived={
                        setIsLatestMotionMetricReceived
                      }
                      isListeningForRealtimeMetrics={enableRealtimeUpdates}
                      isLatestMotionMeticReceived={isLatestMotionMetricReceived}
                    />
                  )}
                  {metricType === "air_quality" && (
                    <AirQualityCharts
                      setIsLatestAirQualityMetricReceived={
                        setIsLatestAirQualityMetricReceived
                      }
                      isLatestAirQualityMetricReceived={
                        isLatestAirQualityMetricReceived
                      }
                      isListeningForRealtimeMetrics={false}
                      timestamp={to}
                    />
                  )}
                </div>
              </div>
            </div>
          )}

          {mainView === "floorplan" && (
            <div
              className="bg-white rounded-lg border shadow-sm p-6"
              style={{ width: "100%" }}
            >
              <h2 className="text-2xl font-bold mb-6">План помещения</h2>
              <FloorPlan
                isListeningForRealtimeMetrics={enableRealtimeUpdates}
                timestamp={to}
                signalRMessage={message}
              />
            </div>
          )}
        </div>
      </div>
    </main>
  );
}

export function formatDate(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  const hours = String(date.getHours()).padStart(2, "0");
  const minutes = String(date.getMinutes()).padStart(2, "0");

  return `${year}-${month}-${day}T${hours}:${minutes}`;
}
