import { AirQualitySummary } from "@/app/models/air-quality-summary";
import { apolloServerClient } from "@/services/apollo";
import { GET_AIR_QUALITY_SUMMARY } from "@/services/graph-ql/queries/get-air-quality-metrics";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Bar, Radar } from "react-chartjs-2";
import { ChartData } from "chart.js";

export function AirQualityCharts({
  setIsLatestAirQualityMetricReceived,
  isLatestAirQualityMetricReceived,
  isListeningForRealtimeMetrics,
  timestamp,
}: {
  setIsLatestAirQualityMetricReceived: (value: boolean) => void;
  isListeningForRealtimeMetrics: boolean;
  isLatestAirQualityMetricReceived: boolean;
  timestamp: Date;
}) {
  const [airQualitySummary, setAirQualitySummary] = useState<
    AirQualitySummary[]
  >([]);

  // -------------------------------
  // FETCH FUNCTION
  // -------------------------------
  const fetchAirSummary = useCallback(
    async (useLatestValue: boolean, timestamp?: Date) => {
      const { data } = await apolloServerClient.query<{
        airQualitySummary: AirQualitySummary[];
      }>({
        query: GET_AIR_QUALITY_SUMMARY,
        variables: {
          timestamp: isListeningForRealtimeMetrics ? undefined : timestamp,
          useLatestValue,
        },
      });

      setAirQualitySummary(data?.airQualitySummary ?? []);
    },
    [isListeningForRealtimeMetrics]
  );

  // -------------------------------
  // INITIAL LOAD + SWITCH realtime/historical
  // -------------------------------
  useEffect(() => {
    if (isListeningForRealtimeMetrics) {
      (async () => {
        await fetchAirSummary(true);
      })();
    } else {
      (async () => {
        await fetchAirSummary(false, timestamp);
      })();
    }
  }, [fetchAirSummary, isListeningForRealtimeMetrics, timestamp]);

  // -------------------------------
  // REALTIME POLLING WHEN METRIC RECEIVED
  // -------------------------------
  useEffect(() => {
    if (!isLatestAirQualityMetricReceived || !isListeningForRealtimeMetrics) {
      return;
    }

    const timeout = setTimeout(async () => {
      await fetchAirSummary(true);
      setIsLatestAirQualityMetricReceived(false);
    }, 5000);

    return () => clearTimeout(timeout);
  }, [
    setIsLatestAirQualityMetricReceived,
    isListeningForRealtimeMetrics,
    fetchAirSummary,
    isLatestAirQualityMetricReceived,
  ]);

  // -------------------------------
  // BUILD CHART DATA
  // -------------------------------
  const { barChartData, radarChartData } = useMemo(() => {
    if (!airQualitySummary || airQualitySummary.length === 0) {
      return { barChartData: undefined, radarChartData: undefined };
    }

    const rooms = airQualitySummary.map((r) => r.room);

    const barData: ChartData<"bar"> = {
      labels: rooms,
      datasets: [
        {
          label: "CO₂ (ppm)",
          data: airQualitySummary.map((v) => v.co2),
          backgroundColor: "#FF6384",
          borderColor: "#FF6384",
          borderWidth: 2,
        },
        {
          label: "PM2.5 (µg/m³)",
          data: airQualitySummary.map((v) => v.pm25),
          backgroundColor: "#36A2EB",
          borderColor: "#36A2EB",
          borderWidth: 2,
        },
        {
          label: "Humidity (%)",
          data: airQualitySummary.map((v) => v.humidity),
          backgroundColor: "#FFCE56",
          borderColor: "#FFCE56",
          borderWidth: 2,
        },
      ],
    };

    // Radar Chart
    const maxCo2 = Math.max(...airQualitySummary.map((v) => v.co2), 1);
    const maxPm25 = Math.max(...airQualitySummary.map((v) => v.pm25), 1);
    const maxHumidity = Math.max(
      ...airQualitySummary.map((v) => v.humidity),
      1
    );

    const colors = ["#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF"];

    const radarData: ChartData<"radar"> = {
      labels: ["CO₂", "PM2.5", "Humidity"],
      datasets: airQualitySummary.map((roomData, index) => ({
        label: roomData.room,
        data: [
          (roomData.co2 / maxCo2) * 100,
          (roomData.pm25 / maxPm25) * 100,
          (roomData.humidity / maxHumidity) * 100,
        ],
        backgroundColor: `${colors[index % colors.length]}40`,
        borderColor: colors[index % colors.length],
        borderWidth: 2,
      })),
    };

    return { barChartData: barData, radarChartData: radarData };
  }, [airQualitySummary]);

  // -------------------------------
  // RENDER
  // -------------------------------
  return (
    <div className="space-y-6">
      <h3 className="text-xl font-bold">Air Quality</h3>

      {airQualitySummary.length === 0 && (
        <div className="bg-white p-4 rounded-lg border">
          <p className="text-gray-500">No Data</p>
        </div>
      )}

      {airQualitySummary.length > 0 && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {barChartData && (
            <div className="bg-white p-4 rounded-lg border">
              <h4 className="font-semibold mb-4">Per Room</h4>
              <Bar
                data={barChartData}
                options={{
                  responsive: true,
                  scales: {
                    y: { beginAtZero: true },
                  },
                }}
              />
            </div>
          )}

          {radarChartData && (
            <div className="bg-white p-4 rounded-lg border">
              <h4 className="font-semibold mb-4">Parameters Comparison</h4>
              <Radar
                data={radarChartData}
                options={{
                  responsive: true,
                  scales: {
                    r: { beginAtZero: true, max: 100 },
                  },
                }}
              />
            </div>
          )}
        </div>
      )}
    </div>
  );
}
