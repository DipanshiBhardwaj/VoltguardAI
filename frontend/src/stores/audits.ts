import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { api } from '@/api/client'
import type { AuditReport, BillSummary } from '@/api/types'

interface RunAuditInput {
  billingCyclePeriod: string
  fromDate: string
  toDate: string
}

export const useAuditsStore = defineStore('audits', () => {
  const reports = ref<AuditReport[]>([])

  // Reports still waiting on the AI agent — drives the sidebar badge.
  const pendingCount = computed(() => reports.value.filter((r) => r.status === 5).length)

  async function fetchAll() {
    const { data } = await api.get<AuditReport[]>('/api/v1/audit-reports')
    reports.value = data
  }

  async function fetchOne(auditReportId: number): Promise<AuditReport> {
    const { data } = await api.get<AuditReport>(`/api/v1/audit-reports/${auditReportId}`)
    return data
  }

  async function runAudit(meterAssetId: number, input: RunAuditInput): Promise<BillSummary> {
    const { data } = await api.post<BillSummary>(`/api/v1/meters/${meterAssetId}/audit`, input)
    return data
  }

  return { reports, pendingCount, fetchAll, fetchOne, runAudit }
})
