import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '@/api/client'
import type {
  AnomalyReading,
  BillForecast,
  IngestionResult,
  Meter,
  SlabCode,
  TelemetryRow,
} from '@/api/types'

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

  async function update(meterAssetId: number, serialNumber: string, slabCode: SlabCode) {
    const { data } = await api.put<Meter>(`/api/v1/meters/${meterAssetId}`, { serialNumber, slabCode })
    const i = meters.value.findIndex((m) => m.meterAssetId === meterAssetId)
    if (i >= 0) meters.value[i] = data
    return data
  }

  async function remove(meterAssetId: number) {
    await api.delete(`/api/v1/meters/${meterAssetId}`)
    meters.value = meters.value.filter((m) => m.meterAssetId !== meterAssetId)
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

  // from/to are ISO instants with an offset (see utils/time); either may be omitted.
  async function fetchReadingsInRange(meterAssetId: number, from?: string, to?: string): Promise<TelemetryRow[]> {
    const { data } = await api.get<TelemetryRow[]>(`/api/v1/meters/${meterAssetId}/readings`, {
      params: { from, to },
    })
    return data
  }

  async function fetchAnomalies(meterAssetId: number, from?: string, to?: string): Promise<AnomalyReading[]> {
    const { data } = await api.get<AnomalyReading[]>(`/api/v1/meters/${meterAssetId}/anomalies`, {
      params: { from, to },
    })
    return data
  }

  async function fetchForecast(meterAssetId: number): Promise<BillForecast> {
    const { data } = await api.get<BillForecast>(`/api/v1/meters/${meterAssetId}/forecast`)
    return data
  }

  return {
    meters,
    fetchAll,
    create,
    update,
    remove,
    upload,
    fetchReadings,
    fetchReadingsInRange,
    fetchAnomalies,
    fetchForecast,
  }
})
