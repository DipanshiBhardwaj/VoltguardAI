<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { Line } from 'vue-chartjs'
import type { ChartData, ChartOptions } from 'chart.js'
import { CHART_COLORS } from '@/charts'
import { useMetersStore } from '@/stores/meters'
import { NORMAL_MAX_VOLTAGE, NORMAL_MIN_VOLTAGE } from '@/api/limits'
import type { TelemetryRow } from '@/api/types'
import { formatIst, istDate, istDayEnd, istDayStart } from '@/utils/time'

// Must match QueryLimitConstants.MaxRangeReadings: the API returns at most this many.
const MAX_RANGE_READINGS = 3000

const metersStore = useMetersStore()

const selectedMeterId = ref<number | null>(null)
const fromDate = ref('')
const toDate = ref('')
const readings = ref<TelemetryRow[]>([])
const loading = ref(false)
const errorMsg = ref<string | null>(null)

const selectedMeter = computed(() => metersStore.meters.find((m) => m.meterAssetId === selectedMeterId.value))

onMounted(async () => {
  await metersStore.fetchAll()
  const first = metersStore.meters[0]
  if (first) selectedMeterId.value = first.meterAssetId
})

// Default to the calendar month (IST) of the meter's latest reading.
watch(selectedMeter, (meter) => {
  if (!meter) return
  if (meter.latestReadingOn) {
    const last = istDate(meter.latestReadingOn)
    fromDate.value = `${last.slice(0, 7)}-01`
    toDate.value = last
  } else {
    fromDate.value = ''
    toDate.value = ''
  }
  load()
})

async function load() {
  if (!selectedMeterId.value) return
  if (!fromDate.value && !toDate.value) {
    readings.value = []
    return
  }
  loading.value = true
  errorMsg.value = null
  try {
    const rows = await metersStore.fetchReadingsInRange(
      selectedMeterId.value,
      fromDate.value ? istDayStart(fromDate.value) : undefined,
      toDate.value ? istDayEnd(toDate.value) : undefined,
    )
    // The API returns newest first; charts read left to right in time.
    readings.value = [...rows].reverse()
  } catch {
    errorMsg.value = 'Could not load readings. Check the date range.'
    readings.value = []
  } finally {
    loading.value = false
  }
}

const labels = computed(() => readings.value.map((r) => formatIst(r.intervalEnding, { year: undefined })))

function lineOptions(yTitle: string, extra: ChartOptions<'line'> = {}): ChartOptions<'line'> {
  return {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    interaction: { mode: 'index', intersect: false },
    elements: { point: { radius: 0 }, line: { borderWidth: 1.5 } },
    scales: {
      x: { ticks: { maxTicksLimit: 8, maxRotation: 0 } },
      y: { title: { display: true, text: yTitle } },
    },
    plugins: { legend: { display: true, position: 'bottom' } },
    ...extra,
  }
}

const powerData = computed<ChartData<'line'>>(() => ({
  labels: labels.value,
  datasets: [
    {
      label: 'Active power (kW)',
      data: readings.value.map((r) => r.activePowerKW),
      borderColor: CHART_COLORS.primary,
      backgroundColor: CHART_COLORS.primary,
    },
  ],
}))

const powerOptions = lineOptions('kW')

// The normal band is drawn as two flat lines with the gap between them shaded.
const voltageData = computed<ChartData<'line'>>(() => ({
  labels: labels.value,
  datasets: [
    {
      label: `Normal band low (${NORMAL_MIN_VOLTAGE} V)`,
      data: readings.value.map(() => NORMAL_MIN_VOLTAGE),
      borderColor: CHART_COLORS.bandLine,
      borderDash: [6, 4],
      borderWidth: 1,
      fill: false,
    },
    {
      label: `Normal band high (${NORMAL_MAX_VOLTAGE} V)`,
      data: readings.value.map(() => NORMAL_MAX_VOLTAGE),
      borderColor: CHART_COLORS.bandLine,
      backgroundColor: CHART_COLORS.band,
      borderDash: [6, 4],
      borderWidth: 1,
      fill: '-1',
    },
    {
      label: 'Voltage (V)',
      data: readings.value.map((r) => r.voltageV),
      borderColor: CHART_COLORS.secondary,
      backgroundColor: CHART_COLORS.secondary,
    },
  ],
}))

const voltageOptions = lineOptions('V', {
  scales: {
    x: { ticks: { maxTicksLimit: 8, maxRotation: 0 } },
    y: { title: { display: true, text: 'V' }, suggestedMin: 200, suggestedMax: 250 },
  },
})
</script>

<template>
  <div class="space-y-6 max-w-6xl">
    <header class="space-y-2">
      <h1 class="text-3xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
        Consumption
      </h1>
      <p class="text-slate-500">Power drawn and supply voltage over time, from your 15-minute readings. Times are IST.</p>
    </header>

    <section class="glass glass-border rounded-xl p-6 grid gap-4 sm:grid-cols-4 items-end">
      <div class="sm:col-span-2">
        <label class="block text-xs text-slate-400 mb-1" for="consumption-meter">Meter</label>
        <select
          id="consumption-meter"
          v-model="selectedMeterId"
          class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800"
        >
          <option v-for="m in metersStore.meters" :key="m.meterAssetId" :value="m.meterAssetId">
            {{ m.serialNumber }}
          </option>
        </select>
      </div>
      <div>
        <label class="block text-xs text-slate-400 mb-1" for="consumption-from">From</label>
        <input id="consumption-from" v-model="fromDate" type="date" class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800" />
      </div>
      <div>
        <label class="block text-xs text-slate-400 mb-1" for="consumption-to">To</label>
        <input id="consumption-to" v-model="toDate" type="date" class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800" />
      </div>
      <div class="sm:col-span-4">
        <button
          :disabled="loading || !selectedMeterId"
          class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-smooth"
          @click="load"
        >
          {{ loading ? 'Loading…' : 'Show readings' }}
        </button>
      </div>
    </section>

    <p v-if="metersStore.meters.length === 0" class="text-sm text-amber-600">No meters yet. Add one from the dashboard first.</p>
    <p v-if="errorMsg" class="text-sm text-red-600">{{ errorMsg }}</p>
    <p v-if="readings.length >= MAX_RANGE_READINGS" class="text-sm text-amber-600">
      Showing the latest {{ MAX_RANGE_READINGS }} readings in this range. Pick a shorter range to see all of it.
    </p>

    <div v-if="!loading && readings.length === 0 && !errorMsg" class="glass glass-border rounded-xl p-10 text-center text-slate-400">
      No readings in this period. Upload a ledger first.
    </div>

    <template v-else-if="readings.length > 0">
      <section class="glass glass-border rounded-xl p-6 space-y-3">
        <h2 class="text-lg font-bold text-slate-900">Active power</h2>
        <div class="h-72">
          <Line :data="powerData" :options="powerOptions" />
        </div>
      </section>

      <section class="glass glass-border rounded-xl p-6 space-y-3">
        <h2 class="text-lg font-bold text-slate-900">Supply voltage</h2>
        <p class="text-xs text-slate-500">
          Shaded band: normal supply, {{ NORMAL_MIN_VOLTAGE }}–{{ NORMAL_MAX_VOLTAGE }} V (230 V ± 6%).
          Readings outside it appear in the Anomaly Log.
        </p>
        <div class="h-72">
          <Line :data="voltageData" :options="voltageOptions" />
        </div>
      </section>
    </template>
  </div>
</template>
