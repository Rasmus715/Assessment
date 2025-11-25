import { Metric } from "@/app/models/metric.model";
import {
  aggregatePowerConsumption,
  PowerConsumptionSummary,
} from "@/app/models/power-consumption-summary.model";
import { apolloServerClient } from "@/services/apollo";
import { GET_POWER_CONSUMPTION_SUMMARY } from "@/services/graph-ql/queries/get-power-consumption-summary";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Bar, Doughnut, Line } from "react-chartjs-2";

export function EnergyCharts({
  setIsLatestEnergyMeticReceived,
  isListeningForRealtimeMetrics,
  isLatestEnergyMeticReceived,
  from,
  to,
}: {
  fetchMetrics: any;
  room: any;
  from: any;
  to: any;
  isListeningForRealtimeMetrics: boolean;
  isLatestEnergyMeticReceived: boolean;
  setIsLatestEnergyMeticReceived: any;
}) {
  const [summary, setSummary] = useState<PowerConsumptionSummary[]>([]);

  const [barData, setBarData] = useState<any>(null);
  const [pieData, setPieData] = useState<any>(null);
  const [totalEnergy, setTotalEnergy] = useState<number>(0);

  const fetchPowerConsumptionSummary = useCallback(
    async (from?: string, to?: string) => {
      //   if (isLoading) {
      //     return;
      //   }

      //   setIsLoading(true);
      //   setError(null);

      const { data } = await apolloServerClient.query<{
        powerConsumptionSummary: PowerConsumptionSummary[];
      }>({
        query: GET_POWER_CONSUMPTION_SUMMARY,
        variables: {
          from: !isListeningForRealtimeMetrics ? from : undefined,
          to: !isListeningForRealtimeMetrics ? to : undefined,
        },
      });

      setSummary(data?.powerConsumptionSummary ?? []);
    },
    [isListeningForRealtimeMetrics]
  );

  useEffect(() => {
    if (isListeningForRealtimeMetrics) {
      (async () => {
        await fetchPowerConsumptionSummary();
      })();
    } else {
      (async () => {
        await fetchPowerConsumptionSummary(from, to);
      })();
    }
  }, [fetchPowerConsumptionSummary, from, isListeningForRealtimeMetrics, to]);

  useEffect(() => {
    if (!isLatestEnergyMeticReceived || isListeningForRealtimeMetrics) {
      return;
    }

    const timeout = setTimeout(async () => {
      await fetchPowerConsumptionSummary();
      setIsLatestEnergyMeticReceived(false);
    }, 5000);

    return () => clearTimeout(timeout);
  }, [
    isLatestEnergyMeticReceived,
    fetchPowerConsumptionSummary,
    setIsLatestEnergyMeticReceived,
    isListeningForRealtimeMetrics,
  ]);

  useEffect(() => {
    if (!summary) return;

    const labels = summary.map((s) => s.room);
    const values = summary.map((s) => Number(s.energy));

    // Total consumption
    setTotalEnergy(values.reduce((acc, x) => acc + x, 0));

    // Bar chart data
    setBarData({
      labels,
      datasets: [
        {
          label: "Power consumption (w/h)",
          data: values,
          backgroundColor: [
            "#FF6384",
            "#36A2EB",
            "#FFCE56",
            "#4BC0C0",
            "#9966FF",
            "#FF9F40",
          ],
          borderColor: [
            "#FF6384",
            "#36A2EB",
            "#FFCE56",
            "#4BC0C0",
            "#9966FF",
            "#FF9F40",
          ],
          borderWidth: 2,
        },
      ],
    });

    // Pie chart data
    setPieData({
      labels,
      datasets: [
        {
          label: "Consumption percentages",
          data: values,
          backgroundColor: [
            "#FF6384",
            "#36A2EB",
            "#FFCE56",
            "#4BC0C0",
            "#9966FF",
            "#FF9F40",
          ],
          borderWidth: 2,
        },
      ],
    });
  }, [summary]);

  return (
    <div className="space-y-6">
      <h3 className="text-xl font-bold">Power consumption</h3>

      {summary?.length == 0 && (
        <div className="space-y-6">
          <div className="bg-white p-4 rounded-lg border">
            <p className="text-gray-500">No Data</p>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 d-flex">
        {summary.length > 0 && !!barData && (
          <div className="bg-white p-4 rounded-lg border">
            <h4 className="font-semibold mb-4">
              Power consumption distribution per room
            </h4>
            <Bar
              data={barData}
              options={{
                responsive: true,
                plugins: {
                  legend: { display: false },
                  tooltip: {
                    callbacks: {
                      label: (context) =>
                        `${(context.parsed.y ?? 0).toFixed(2)} w/h`,
                    },
                  },
                },
              }}
            />
          </div>
        )}

        {summary.length > 0 && !!pieData && (
          <div className="bg-white p-4 rounded-lg border">
            <h4 className="font-semibold mb-4">
              Power consumption percentages
            </h4>
            <Doughnut
              data={pieData}
              options={{
                responsive: true,
                cutout: "60%",
                plugins: {
                  tooltip: {
                    callbacks: {
                      label: (context) => {
                        const value = context.parsed || 0;
                        const percentage = (
                          (value / totalEnergy) *
                          100
                        ).toFixed(1);
                        return `${context.label}: ${value.toFixed(
                          2
                        )} w/h (${percentage}%)`;
                      },
                    },
                  },
                },
              }}
            />
          </div>
        )}
      </div>
    </div>
  );
}
