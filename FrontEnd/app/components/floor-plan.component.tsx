"use client";

import { useState, useEffect, useRef, useCallback, useMemo } from "react";
import { apolloServerClient } from "@/services/apollo";
import { GET_LATEST_TELEMETRY } from "@/services/graph-ql/queries/get-latest-telemetry";
import { Metric } from "@/app/models/metric.model";

type Room = {
  name: string;
  x: number;
  y: number;
  width: number;
  height: number;
  color: string;
  group?: string;
};

type TelemetryItem = {
  key: string;
  value: string;
};

type TelemetryData = {
  room: string;
  telemetry: TelemetryItem[];
};

export function FloorPlan({
  isListeningForRealtimeMetrics,
  timestamp,
  signalRMessage,
}: {
  isListeningForRealtimeMetrics: boolean;
  timestamp: Date;
  signalRMessage?: Metric;
}) {
  const [hoveredRoom, setHoveredRoom] = useState<string | null>(null);
  const [allTelemetry, setAllTelemetry] = useState<Map<string, TelemetryData>>(
    new Map()
  );
  const [popupPosition, setPopupPosition] = useState({ x: 0, y: 0 });
  const [isLoading, setIsLoading] = useState(false);
  const [highlightedRooms, setHighlightedRooms] = useState<Map<string, string>>(
    new Map()
  );
  const svgRef = useRef<SVGSVGElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const timeoutRefs = useRef<Map<string, NodeJS.Timeout>>(new Map());

  const rooms: Room[] = useMemo(
    () => [
      {
        name: "Bedroom",
        x: -68.8 * 3,
        y: 19 * 3,
        width: 56.9 * 3,
        height: 35.6 * 3,
        color: "#E3F2FD",
      },
      {
        name: "Living Room Top",
        x: -11.9 * 3,
        y: 19 * 3,
        width: 64 * 3,
        height: 33 * 3,
        color: "#F3E5F5",
        group: "Living Room",
      },
      {
        name: "Living Room Bottom",
        x: 52 * 3,
        y: 19 * 3,
        width: 48 * 3,
        height: 68 * 3,
        color: "#F3E5F5",
        group: "Living Room",
      },
      {
        name: "Garage",
        x: -68.8 * 3,
        y: 52.3 * 3,
        width: 71.1 * 3,
        height: 56.9 * 3,
        color: "#ECEFF1",
      },
      {
        name: "Corridor",
        x: 2.32 * 3,
        y: 52.3 * 3,
        width: 49.8 * 3,
        height: 85.3 * 3,
        color: "#FFF9C4",
      },
      {
        name: "Kitchen",
        x: 52.1 * 3,
        y: 87.8 * 3,
        width: 49.8 * 3,
        height: 49.8 * 3,
        color: "#FFE0B2",
      },
      {
        name: "Office",
        x: -68.8 * 3,
        y: 109 * 3,
        width: 71.1 * 3,
        height: 28.4 * 3,
        color: "#C8E6C9",
      },
    ],
    []
  );

  const minX = Math.min(...rooms.map((r) => r.x));
  const minY = Math.min(...rooms.map((r) => r.y));
  const maxX = Math.max(...rooms.map((r) => r.x + r.width));
  const maxY = Math.max(...rooms.map((r) => r.y + r.height));
  const svgWidth = maxX - minX;
  const svgHeight = maxY - minY;

  // Загрузка телеметрии при инициализации и при изменении параметров
  const loadTelemetry = useCallback(async () => {
    setIsLoading(true);
    try {
      const { data } = await apolloServerClient.query<{
        latestTelemetry: TelemetryData[];
      }>({
        query: GET_LATEST_TELEMETRY,
        variables: {
          useLatestValue: isListeningForRealtimeMetrics,
          timestamp: isListeningForRealtimeMetrics ? undefined : timestamp,
        },
        fetchPolicy: "no-cache",
      });

      // Сохраняем все данные телеметрии в Map по комнатам
      const telemetryMap = new Map<string, TelemetryData>();
      data?.latestTelemetry?.forEach((item) => {
        telemetryMap.set(item.room, item);
      });

      setAllTelemetry(telemetryMap);
    } catch (error) {
      console.error("Failed to load telemetry:", error);
    } finally {
      setIsLoading(false);
    }
  }, [isListeningForRealtimeMetrics, timestamp]);

  useEffect(() => {
    loadTelemetry();
  }, [loadTelemetry]);

  // Cleanup всех таймеров при размонтировании компонента
  useEffect(() => {
    const timeouts = timeoutRefs.current;
    return () => {
      timeouts.forEach((timeout) => clearTimeout(timeout));
      timeouts.clear();
    };
  }, []);

  // Обработка SignalR сообщений для изменения цвета комнат
  useEffect(() => {
    if (!signalRMessage || !isListeningForRealtimeMetrics) {
      return;
    }

    const metric = signalRMessage;
    if (!metric.room) {
      return;
    }

    // Находим комнату по имени или группе
    const room = rooms.find(
      (r) => r.name === metric.room || r.group === metric.room
    );

    if (!room) {
      return;
    }

    const roomKey = room.group || room.name;
    const highlightColor = "#4CAF50"; // Зеленый цвет для подсветки
    const timeouts = timeoutRefs.current;

    // Обновляем телеметрию для этой комнаты
    setAllTelemetry((prev) => {
      const newMap = new Map(prev);
      const existingTelemetry = newMap.get(roomKey) || {
        room: roomKey,
        telemetry: [],
      };

      // Преобразуем поля Metric в формат TelemetryItem[]
      const telemetryItems: TelemetryItem[] = [...existingTelemetry.telemetry];

      // Обновляем или добавляем значения из текущего сообщения
      if (!!metric.energy) {
        console.warn(metric);
        const index = telemetryItems.findIndex((item) => item.key === "energy");
        if (index >= 0) {
          telemetryItems[index].value = metric.energy.toString();
        } else {
          telemetryItems.push({
            key: "energy",
            value: metric.energy?.toString() ?? "-",
          });
        }
      }

      if (!!metric.motionDetected) {
        const index = telemetryItems.findIndex(
          (item) => item.key === "motionDetected"
        );
        if (index >= 0) {
          telemetryItems[index].value =
            metric.motionDetected?.toString() ?? false;
        } else {
          telemetryItems.push({
            key: "motionDetected",
            value: metric.motionDetected.toString(),
          });
        }
      }

      if (!!metric.co2) {
        const index = telemetryItems.findIndex((item) => item.key === "co2");
        if (index >= 0) {
          telemetryItems[index].value = metric.co2.toString();
        } else {
          telemetryItems.push({ key: "co2", value: metric.co2.toString() });
        }
      }

      if (!!metric.pm25) {
        const index = telemetryItems.findIndex((item) => item.key === "pm25");
        if (index >= 0) {
          telemetryItems[index].value = metric.pm25.toString();
        } else {
          telemetryItems.push({ key: "pm25", value: metric.pm25.toString() });
        }
      }

      if (!!metric.humidity) {
        const index = telemetryItems.findIndex(
          (item) => item.key === "humidity"
        );
        if (index >= 0) {
          telemetryItems[index].value = metric.humidity.toString();
        } else {
          telemetryItems.push({
            key: "humidity",
            value: metric.humidity.toString(),
          });
        }
      }

      newMap.set(roomKey, {
        room: roomKey,
        telemetry: telemetryItems,
      });

      return newMap;
    });

    // Очищаем предыдущий таймер для этой комнаты, если он есть
    const existingTimeout = timeouts.get(roomKey);
    if (existingTimeout) {
      clearTimeout(existingTimeout);
      timeouts.delete(roomKey);
    }

    // Устанавливаем цвет подсветки
    setHighlightedRooms((prev) => {
      const newMap = new Map(prev);
      newMap.set(roomKey, highlightColor);
      return newMap;
    });

    // Возвращаем к дефолтному цвету через 1 секунду с fade эффектом
    const timeoutId = setTimeout(() => {
      // Проверяем, что таймер все еще актуален перед удалением
      const currentTimeout = timeouts.get(roomKey);
      if (currentTimeout === timeoutId) {
        setHighlightedRooms((prev) => {
          const newMap = new Map(prev);
          newMap.delete(roomKey);
          return newMap;
        });
        timeouts.delete(roomKey);
      }
    }, 1000);

    timeouts.set(roomKey, timeoutId);
  }, [signalRMessage, isListeningForRealtimeMetrics, rooms]);

  // Обновление позиции попапа при движении мыши
  const handleMouseMove = (e: React.MouseEvent<SVGSVGElement>) => {
    // Используем координаты относительно viewport для fixed позиционирования
    setPopupPosition({
      x: e.clientX + 15,
      y: e.clientY - 10,
    });
  };

  const hoveredTelemetry = hoveredRoom ? allTelemetry.get(hoveredRoom) : null;

  return (
    <div
      ref={containerRef}
      className="overflow-auto flex justify-center relative"
    >
      <svg
        ref={svgRef}
        width={svgWidth * 3}
        height={600}
        viewBox={`${minX} ${minY} ${svgWidth} ${svgHeight}`}
        onMouseMove={handleMouseMove}
      >
        {/* Заливки комнат с hover */}
        {rooms.map((room) => {
          const roomKey = room.group || room.name;
          const currentColor = highlightedRooms.get(roomKey) || room.color;
          return (
            <g
              key={room.name}
              onMouseEnter={() => setHoveredRoom(roomKey)}
              onMouseLeave={() => setHoveredRoom(null)}
              style={{ cursor: "pointer" }}
            >
              <rect
                x={room.x}
                y={room.y}
                width={room.width}
                height={room.height}
                fill={currentColor}
                fillOpacity={0.7}
                style={{
                  transition: "fill 1s ease-in-out",
                }}
              />
              <text
                x={room.x + room.width / 2}
                y={room.y + room.height / 2}
                textAnchor="middle"
                dominantBaseline="middle"
                fontSize="8"
                fontWeight="600"
                fill="#1a1a1a"
              >
                {room.group || room.name}
              </text>
            </g>
          );
        })}

        {/* Черные стены / перекрытия */}
        <g fill="none" stroke="#000" strokeWidth="2">
          {/* Верхняя стена над Bedroom и Living Room */}
          <path d="M-206.4 57 H300" />
          {/* Остальные стены из твоего SVG */}
          <path d="m-206.4 263.4v149.4h255.9" />
          <g strokeWidth="1.9">
            <path d="m-206.4 156.9h63.9" />
            <path d="m-78.3 156.9h42.6v-106.8" />
            <path d="m-35.7 156.9h42.6v42.6" />
            <path d="m6.96 263.4v63.9h-85.2" />
            <path d="m-142.5 327h-63.9" />
            <path d="m6.96 327v85.2" />
            <path d="m6.96 156.9h42.6" />
            <path d="m156.3 156.9v42.6" />
            <path d="m306 263.4h-42.6" />
            <path d="m198.9 263.4h-42.6v149.4" />
          </g>
        </g>
      </svg>

      {/* Попап с телеметрией */}
      {hoveredRoom && hoveredTelemetry && (
        <div
          className="fixed bg-white border border-gray-300 rounded-lg shadow-lg p-4 min-w-[200px] pointer-events-none"
          style={{
            left: `${popupPosition.x}px`,
            top: `${popupPosition.y}px`,
            transform: "translateY(-100%)",
            zIndex: 9999,
          }}
        >
          <h3 className="font-bold text-sm mb-2 text-gray-800 border-b pb-1">
            {hoveredRoom}
          </h3>
          {isLoading ? (
            <div className="text-sm text-gray-500">Загрузка...</div>
          ) : hoveredTelemetry.telemetry &&
            hoveredTelemetry.telemetry.length > 0 ? (
            <div className="space-y-1">
              {hoveredTelemetry.telemetry.map((item, index) => (
                <div
                  key={index}
                  className="text-xs flex justify-between gap-4 text-gray-700"
                >
                  <span className="font-medium">{item.key}:</span>
                  <span className="text-gray-600">{item.value}</span>
                </div>
              ))}
            </div>
          ) : (
            <div className="text-sm text-gray-500">Нет данных</div>
          )}
        </div>
      )}
    </div>
  );
}
