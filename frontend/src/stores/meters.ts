import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '@/api/client'
import type { Meter, TelemetryRow, IngestionResult, SlabCode } from '@/api/types'

export const useMetersStore = defineStore('meters', () => {
  const meters = ref<Meter[]>([])

  async function fetchAll() {
    const { data } = await api.get<Meter[]>('/api/v1/meters')
    meters.value = data
  }

  async function create(serialNumber: string, slabCode: SlabCode) {
    const { data } = await api.post<Meter>('/api/v1/meters', { serialNumber, slabCode })
    meters.value.push(data)
    return data
  }

  async function upload(meterAssetId: number, file: File): Promise<IngestionResult> {
    const form = new FormData()
    form.append('file', file)
    const { data } = await api.post<IngestionResult>(
      `/api/v1/meters/${meterAssetId}/telemetry`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    )
    return data
  }

  async function fetchReadings(meterAssetId: number, take = 100): Promise<TelemetryRow[]> {
    const { data } = await api.get<TelemetryRow[]>(`/api/v1/meters/${meterAssetId}/readings`, {
      params: { take },
    })
    return data
  }

  return { meters, fetchAll, create, upload, fetchReadings }
})
