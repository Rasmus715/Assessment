import { Metric } from "./metric.model";

export interface PowerConsumptionSummary {
  room: string;
  energy: number;
}

export function aggregatePowerConsumption(
  metrics: Metric[]
): PowerConsumptionSummary[] {
  const roomEnergy: Record<string, number> = {};

  metrics.forEach((m) => {
    if (m.type === "energy" && m.energy !== undefined) {
      roomEnergy[m.room] = (roomEnergy[m.room] || 0) + m.energy;
    }
  });

  return Object.entries(roomEnergy).map(([room, energy]) => ({
    room,
    energy,
  }));
}
