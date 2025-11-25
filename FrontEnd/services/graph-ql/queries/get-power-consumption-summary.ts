import { gql } from "@apollo/client";

export const GET_POWER_CONSUMPTION_SUMMARY = gql`
  query GetPowerConsumptionMetrics($from: DateTime, $to: DateTime) {
    powerConsumptionSummary(from: $from, to: $to) {
      room
      energy
    }
  }
`;
