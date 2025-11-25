import { gql } from "@apollo/client";

export const GET_METRICS = gql`
  query GetMetrics(
    $room: String
    $type: String
    $from: DateTime
    $to: DateTime
    $skip: Int
    $take: Int
  ) {
    metrics(
      room: $room
      type: $type
      from: $from
      to: $to
      skip: $skip
      take: $take
    ) {
      type
      room
      time

      ... on EnergyEvent {
        energy
      }

      ... on MotionEvent {
        motionDetected
      }

      ... on AirQualityEvent {
        co2
        pm25
        humidity
      }
    }
  }
`;
