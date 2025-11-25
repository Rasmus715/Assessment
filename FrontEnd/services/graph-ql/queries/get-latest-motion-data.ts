import { gql } from "@apollo/client";

export const GET_LATEST_MOTION_DATA = gql`
  query GetLatestMotionData($timestamp: DateTime, $useLatestValue: Boolean) {
    latestMotionData(timestamp: $timestamp, useLatestValue: $useLatestValue) {
      room
      motionDetected
    }
  }
`;
