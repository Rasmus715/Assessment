import { gql } from "@apollo/client";

export const GET_LATEST_TELEMETRY = gql`
  query GetLatestTelemetry($timestamp: DateTime, $useLatestValue: Boolean!) {
    latestTelemetry(timestamp: $timestamp, useLatestValue: $useLatestValue) {
      room
      telemetry {
        key
        value
      }
    }
  }
`;
