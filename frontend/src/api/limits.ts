// Values shared with the backend. Keep in sync with
// backend/Voltaguard.Common/Constants (ValidationLimitConstants, TimeZoneConstants).

import type { AnomalyType } from './types'

// Normal supply band: 230 V +/- 6%.
export const NORMAL_MIN_VOLTAGE = 216.2
export const NORMAL_MAX_VOLTAGE = 243.8

export const IST_TIME_ZONE = 'Asia/Kolkata'
export const IST_OFFSET = '+05:30'

export const ANOMALY_LABELS: Record<AnomalyType, string> = {
  5: 'Voltage sag',
  10: 'Voltage swell',
  15: 'Current mismatch',
}
