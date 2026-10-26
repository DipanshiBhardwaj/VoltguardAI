// API response types — mirror the C# DTOs in backend/Voltaguard.Common/Dtos.
// Keep these in sync when a DTO changes server-side.

export interface CurrentUser {
  userId: number
  email: string
  fullName: string
  role: string
  token: string
}

// 5 = DomesticLifeline, 10 = DomesticStandard (SlabCode enum)
export type SlabCode = 5 | 10

export interface Meter {
  meterAssetId: number
  serialNumber: string
  slabCode: SlabCode
  readingCount: number
  latestReadingOn: string | null
}

export interface TelemetryRow {
  logId: number
  intervalEnding: string
  voltageV: number
  activePowerKW: number
  powerFactor: number
  frequencyHz: number
  cumulativeKWH: number
}

export interface RowError {
  lineNumber: number
  reason: string
}

export interface IngestionResult {
  acceptedRows: number
  rejectedRows: number
  errors: RowError[]
}

export interface SlabCharge {
  fromUnit: number
  toUnit: number | null
  ratePerUnit: number
  unitsCharged: number
  charge: number
}

export interface ConsumptionFacts {
  peakHour: number
  morningKwh: number
  dayKwh: number
  eveningKwh: number
  nightKwh: number
  highestDemandKW: number
  highestDemandOn: string | null
  averageDailyKwh: number
}

export interface BillSummary {
  auditReportId: number
  billingCyclePeriod: string
  totalUnits: number
  totalCost: number
  slabCharges: SlabCharge[]
  facts: ConsumptionFacts
}

// 5 = Pending, 10 = Completed (AuditReportStatus enum)
export type AuditReportStatus = 5 | 10

export interface AuditReport {
  auditReportId: number
  meterAssetId: number
  billingCyclePeriod: string
  calculatedDeterministicCost: number
  totalUnits: number
  facts: ConsumptionFacts
  aiAnalysisFindings: string | null
  status: AuditReportStatus
  createdOn: string
}
