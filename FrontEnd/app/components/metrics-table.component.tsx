import { useEffect, useRef, useState } from "react";
import {
  Table,
  TableBody,
  TableHead,
  TableHeader,
  TableRow,
  TableCell,
} from "@/components/ui/table";
import { Metric } from "../models/metric.model";
import { randomUUID } from "crypto";
import { uuid } from "uuidv4";

export function MetricsTable({
  fetchMetrics,
  room,
  type,
  from,
  to,
  take,
  enableRealtimeUpdates,
  realtimeMetrics,
}: {
  fetchMetrics: any;
  room: any;
  type: any;
  from: any;
  to: any;
  take: number;
  enableRealtimeUpdates: boolean;
  realtimeMetrics: Metric[];
}) {
  const [metrics, setMetrics] = useState<Metric[]>([]);
  const [skip, setSkip] = useState(0);
  const [isLoadingMore, setIsLoadingMore] = useState(false);

  const scrollRef = useRef<HTMLDivElement>(null);

  const handleScroll = () => {
    const div = scrollRef.current;
    if (!div || isLoadingMore || enableRealtimeUpdates) {
      return;
    }

    const isBottom = div.scrollHeight - div.scrollTop <= div.clientHeight + 200;

    if (isBottom) {
      loadMore();
    }
  };

  useEffect(() => {
    setMetrics([]);
    setSkip(0);

    if (!enableRealtimeUpdates) {
      loadInitial();
    }

    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [room, from, to, enableRealtimeUpdates]);

  useEffect(() => {
    if (enableRealtimeUpdates && realtimeMetrics.length > 0) {
      setMetrics(realtimeMetrics);
    }
  }, [realtimeMetrics, enableRealtimeUpdates]);

  // ------------------------------------------
  // 🔥 RESET ЕСЛИ СМЕНИЛИСЬ ФИЛЬТРЫ
  // ------------------------------------------
  useEffect(() => {
    if (enableRealtimeUpdates) {
      setMetrics(realtimeMetrics);
    } else {
      setMetrics([]);
    }
    setSkip(0);

    loadInitial(); // загрузка первых 50
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [room, type, from, to]);

  // Отдельная функция начальной загрузки
  const loadInitial = async () => {
    if (isLoadingMore || enableRealtimeUpdates) return;

    setIsLoadingMore(true);
    const newMetrics = await fetchMetrics(room, type, from, to, 0, take);

    setMetrics(newMetrics ?? []);
    setSkip(take);
    setIsLoadingMore(false);
  };

  // ------------------------------------------
  // 🔥 INFINITE SCROLL LOADER
  // ------------------------------------------
  const loadMore = async () => {
    if (isLoadingMore) return;

    setIsLoadingMore(true);

    const newMetrics = await fetchMetrics(room, type, from, to, skip, take);

    if (newMetrics?.length) {
      setMetrics((prev) => [...prev, ...newMetrics]);
      setSkip((prev) => prev + take);
    }

    setIsLoadingMore(false);
  };

  return (
    <div
      ref={scrollRef}
      onScroll={handleScroll}
      className="flex-1 overflow-auto rounded-lg border shadow-sm"
    >
      <Table noWrapper>
        <TableHeader className="bg-background sticky top-0 z-10">
          <TableRow>
            <TableHead>Type</TableHead>
            <TableHead>Room</TableHead>
            <TableHead>Time</TableHead>
            <TableHead>Energy</TableHead>
            <TableHead>Motion</TableHead>
            <TableHead>CO₂</TableHead>
            <TableHead>PM2.5</TableHead>
            <TableHead>Humidity</TableHead>
          </TableRow>
        </TableHeader>

        <TableBody>
          {metrics.length === 0 && !isLoadingMore ? (
            <TableRow>
              <TableCell colSpan={8} className="text-center py-8">
                No metrics found
              </TableCell>
            </TableRow>
          ) : (
            metrics.map((m) => (
              <TableRow key={`${m.room}-${m.type}-${m.time}-${uuid()}`}>
                <TableCell className="font-medium">{m.type}</TableCell>
                <TableCell>{m.room}</TableCell>
                <TableCell>{m.time}</TableCell>
                <TableCell>{m.energy ?? "-"}</TableCell>
                <TableCell>
                  {m.motionDetected !== undefined && m.motionDetected !== null
                    ? m.motionDetected.toString()
                    : "-"}
                </TableCell>
                <TableCell>{m.co2 ?? "-"}</TableCell>
                <TableCell>{m.pm25 ?? "-"}</TableCell>
                <TableCell>{m.humidity ?? "-"}</TableCell>
              </TableRow>
            ))
          )}

          {isLoadingMore && (
            <TableRow>
              <TableCell colSpan={8} className="text-center py-4 text-gray-500">
                Loading...
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
