export interface Metric {
  type: string;
  room: string;
  time: string;
  energy?: number;
  motionDetected?: boolean;
  co2?: number;
  pm25?: number;
  humidity?: number;
}
