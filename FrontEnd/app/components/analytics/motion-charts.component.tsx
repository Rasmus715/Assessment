import { Metric } from "@/app/models/metric.model";
import { MotionDetectionSummary } from "@/app/models/motion-detection-summary.model";
import { apolloServerClient } from "@/services/apollo";
import { GET_LATEST_MOTION_DATA } from "@/services/graph-ql/queries/get-latest-motion-data";
import { ChartData } from "chart.js";
import { time, timeStamp } from "console";
import { useCallback, useEffect, useState } from "react";
import { Bar, Doughnut } from "react-chartjs-2";

export function MotionCharts({
  setIsLatestMotionMeticReceived,
  isLatestMotionMeticReceived,
  isListeningForRealtimeMetrics,
  timestamp,
}: {
  setIsLatestMotionMeticReceived: any;
  isListeningForRealtimeMetrics: boolean;
  isLatestMotionMeticReceived: boolean;
  timestamp: Date;
}) {
  const [motionDetectionSummary, setMotionDetectionSummary] = useState<
    MotionDetectionSummary[]
  >([]);

  const [barChartData, setBarChartData] = useState<any>(undefined);
  const [pieChartData, setPieChartData] = useState<any>(undefined);

  const fetchMotionSummary = useCallback(
    async (useLatestValue: boolean, timestamp?: Date) => {
      const { data } = await apolloServerClient.query<{
        latestMotionData: MotionDetectionSummary[];
      }>({
        query: GET_LATEST_MOTION_DATA,
        variables: {
          timestamp: isListeningForRealtimeMetrics ? undefined : timestamp,
          useLatestValue,
        },
      });

      setMotionDetectionSummary(data?.latestMotionData ?? []);
    },
    [isListeningForRealtimeMetrics]
  );

  useEffect(() => {
    if (isListeningForRealtimeMetrics) {
      (async () => {
        await fetchMotionSummary(true);
      })();
    } else {
      (async () => {
        await fetchMotionSummary(false, timestamp);
      })();
    }
  }, [fetchMotionSummary, isListeningForRealtimeMetrics, timestamp]);

  useEffect(() => {
    if (!isLatestMotionMeticReceived || !isListeningForRealtimeMetrics) {
      return;
    }

    const timeout = setTimeout(async () => {
      await fetchMotionSummary(true);
      setIsLatestMotionMeticReceived(false);
    }, 5000);

    return () => clearTimeout(timeout);
  }, [
    setIsLatestMotionMeticReceived,
    isListeningForRealtimeMetrics,
    fetchMotionSummary,
    isLatestMotionMeticReceived,
  ]);

  useEffect(() => {
    setBarChartData({
      labels: motionDetectionSummary.map((summary) => summary.room),
      datasets: [
        {
          label: "Motion Detected",
          data: motionDetectionSummary.map((v) => v.motionDetected),
          backgroundColor: "#4BC0C0",
          borderColor: "#4BC0C0",
          borderWidth: 2,
        },
      ],
    });

    const roomsWithMotion = motionDetectionSummary.filter(
      (v) => v.motionDetected
    ).length;

    const totalRooms = motionDetectionSummary.length;
    const roomsWithoutMotion = totalRooms - roomsWithMotion;

    setPieChartData({
      labels: ["Motion Detected", "No Motion"],
      datasets: [
        {
          label: "Rooms",
          data: [roomsWithMotion, roomsWithoutMotion],
          backgroundColor: ["#4BC0C0", "#FF6384"],
          borderWidth: 2,
        },
      ],
    });
  }, [motionDetectionSummary]);

  return (
    <div className="space-y-6">
      <h3 className="text-xl font-bold">Motion Detection Sensors</h3>

      {motionDetectionSummary?.length == 0 && (
        <div className="space-y-6">
          <div className="bg-white p-4 rounded-lg border">
            <p className="text-gray-500">No Data</p>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {motionDetectionSummary?.length > 0 && barChartData && (
          <div className="bg-white p-4 rounded-lg border">
            <div>
              <h4 className="font-semibold mb-4">Motion Detection by Room</h4>
              <Bar
                data={barChartData}
                options={{
                  responsive: true,
                  scales: {
                    x: { stacked: true },
                    y: { stacked: true, beginAtZero: true },
                  },
                }}
              />
            </div>
          </div>
        )}

        {motionDetectionSummary?.length > 0 && pieChartData && (
          <div className="bg-white p-4 rounded-lg border">
            <div>
              <h4 className="font-semibold mb-4">Motion Detection Stats</h4>
              <Doughnut
                data={pieChartData}
                options={{
                  responsive: true,
                  cutout: "60%",
                  plugins: {
                    tooltip: {
                      callbacks: {
                        label: (context) => {
                          const value = context.parsed || 0;
                          const total = motionDetectionSummary.length;
                          const percentage = ((value / total) * 100).toFixed(1);
                          return `${context.label}: ${value} rooms (${percentage}%)`;
                        },
                      },
                    },
                  },
                }}
              />
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
