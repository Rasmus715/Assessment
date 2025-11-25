import { gql } from "@apollo/client";

export const GET_AIR_QUALITY_SUMMARY = gql`
  query airQualitySummary($timestamp: DateTime, $useLatestValue: Boolean!) {
    airQualitySummary(timestamp: $timestamp, useLatestValue: $useLatestValue) {
      room
      co2
      pm25
      humidity
    }
  }
`;
