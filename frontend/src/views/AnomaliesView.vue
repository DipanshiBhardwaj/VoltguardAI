<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { TriangleAlert } from 'lucide-vue-next'
import { useMetersStore } from '@/stores/meters'
import { ANOMALY_LABELS } from '@/api/limits'
import type { AnomalyReading } from '@/api/types'
import { formatIst, istDayEnd, istDayStart } from '@/utils/time'

const route = useRoute()
const metersStore = useMetersStore()

const selectedMeterId = ref<number | null>(null)
// Empty dates mean "no limit" on that side.
const fromDate = ref('')
const toDate = ref('')
const rows = ref<AnomalyReading[]>([])
const loading = ref(false)
const errorMsg = ref<string | null>(null)

onMounted(async () => {
  await metersStore.fetchAll()
  const requested = Number(route.query.meter)
  const preselected = metersStore.meters.find((m) => m.meterAssetId === requested) ?? metersStore.meters[0]
  if (preselected) selectedMeterId.value = preselected.meterAssetId
})

async function load() {
  if (!selectedMeterId.value) return
  loading.value = true
  errorMsg.value = null
  try {
    rows.value = await metersStore.fetchAnomalies(
      selectedMeterId.value,
      fromDate.value ? istDayStart(fromDate.value) : undefined,
      toDate.value ? istDayEnd(toDate.value) : undefined,
    )
  } catch {
    errorMsg.value = 'Could not load the anomaly log. Check the date range.'
    rows.value = []
  } finally {
    loading.value = false
  }
}

watch(selectedMeterId, load)
</script>

<template>
  <div class="space-y-6 max-w-5xl">
    <header class="space-y-2">
      <h1 class="text-3xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
        Anomaly Log
      </h1>
      <p class="text-slate-500">
        Readings outside the normal 230 V ± 6% supply band, and readings where the phase and neutral
        currents disagree (a sign of tampering or earth leakage). Times are IST.
      </p>
    </header>

    <section class="glass glass-border rounded-xl p-6 grid gap-4 sm:grid-cols-4 items-end">
      <div class="sm:col-span-2">
        <label class="block text-xs text-slate-400 mb-1" for="anomaly-meter">Meter</label>
        <select
          id="anomaly-meter"
          v-model="selectedMeterId"
          class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800"
        >
          <option v-for="m in metersStore.meters" :key="m.meterAssetId" :value="m.meterAssetId">
            {{ m.serialNumber }}
          </option>
        </select>
      </div>
      <div>
        <label class="block text-xs text-slate-400 mb-1" for="anomaly-from">From</label>
        <input id="anomaly-from" v-model="fromDate" type="date" class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800" />
      </div>
      <div>
        <label class="block text-xs text-slate-400 mb-1" for="anomaly-to">To</label>
        <input id="anomaly-to" v-model="toDate" type="date" class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800" />
      </div>
      <div class="sm:col-span-4">
        <button
          :disabled="loading || !selectedMeterId"
          class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-smooth"
          @click="load"
        >
          {{ loading ? 'Loading…' : 'Show anomalies' }}
        </button>
      </div>
    </section>

    <p v-if="metersStore.meters.length === 0" class="text-sm text-amber-600">No meters yet. Add one from the dashboard first.</p>
    <p v-if="errorMsg" class="text-sm text-red-600">{{ errorMsg }}</p>

    <section class="glass glass-border rounded-xl p-6">
      <div v-if="!loading && rows.length === 0 && !errorMsg" class="text-center text-slate-400 py-6">
        No abnormal readings in this period.
      </div>

      <div v-else class="overflow-x-auto">
        <p class="text-sm text-slate-600 mb-3 flex items-center gap-2">
          <TriangleAlert class="w-4 h-4 text-red-500" /> {{ rows.length }} abnormal readings, newest first
        </p>
        <table class="w-full text-sm text-slate-700" data-testid="anomaly-table">
          <thead class="text-xs uppercase text-slate-500 border-b border-slate-200">
            <tr>
              <th class="text-left py-2 pr-4">Time (IST)</th>
              <th class="text-left py-2 pr-4">Type</th>
              <th class="text-right py-2 pr-4">Voltage (V)</th>
              <th class="text-right py-2 pr-4">Phase current (A)</th>
              <th class="text-right py-2">Neutral current (A)</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="r in rows" :key="`${r.logId}-${r.type}`" class="border-b border-slate-100">
              <td class="py-2 pr-4 whitespace-nowrap">{{ formatIst(r.intervalEnding) }}</td>
              <td class="py-2 pr-4">
                <span
                  class="text-xs font-semibold px-2 py-0.5 rounded-full"
                  :class="r.type === 15 ? 'bg-red-100 text-red-700' : 'bg-amber-100 text-amber-700'"
                >
                  {{ ANOMALY_LABELS[r.type] }}
                </span>
              </td>
              <td class="py-2 pr-4 text-right">{{ r.voltageV.toFixed(1) }}</td>
              <td class="py-2 pr-4 text-right">{{ r.phaseCurrentA.toFixed(2) }}</td>
              <td class="py-2 text-right">{{ r.neutralCurrentA.toFixed(2) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
