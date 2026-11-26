<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { isAxiosError } from 'axios'
import { Bar } from 'vue-chartjs'
import type { ChartData, ChartOptions } from 'chart.js'
import { Gauge, Pencil, Plus, Receipt, Trash2, TriangleAlert } from 'lucide-vue-next'
import { CHART_COLORS } from '@/charts'
import { useMetersStore } from '@/stores/meters'
import { useAuditsStore } from '@/stores/audits'
import type { AuditReport, BillForecast, BillSummary, Meter, SlabCode } from '@/api/types'
import { formatIst, istDayEnd, istDayStart } from '@/utils/time'

const router = useRouter()
const metersStore = useMetersStore()
const auditsStore = useAuditsStore()

const showAddMeter = ref(false)
const newSerial = ref('')
const newSlab = ref<SlabCode>(10)
const addError = ref<string | null>(null)

// Meter whose history and projection are shown below the cards.
const selectedMeterId = ref<number | null>(null)
const forecast = ref<BillForecast | null>(null)
const forecastError = ref<string | null>(null)

// Inline edit state for one meter card at a time.
const editingId = ref<number | null>(null)
const editSerial = ref('')
const editSlab = ref<SlabCode>(10)
const editError = ref<string | null>(null)

// Audit panel state, keyed by the meter it belongs to.
const auditMeterId = ref<number | null>(null)
const period = ref('2026-05')
const fromDate = ref('2026-05-01')
const toDate = ref('2026-05-31')
const running = ref(false)
const bill = ref<BillSummary | null>(null)
const auditError = ref<string | null>(null)

onMounted(async () => {
  await Promise.all([metersStore.fetchAll(), auditsStore.fetchAll()])
  const first = metersStore.meters[0]
  if (first) selectedMeterId.value = first.meterAssetId
})

function problemDetail(err: unknown): string | null {
  if (isAxiosError(err) && typeof err.response?.data?.detail === 'string') {
    return err.response.data.detail
  }
  return null
}

function slabLabel(code: SlabCode) {
  return code === 5 ? 'Domestic Lifeline' : 'Domestic Standard'
}

async function addMeter() {
  addError.value = null
  try {
    const meter = await metersStore.create(newSerial.value, newSlab.value)
    newSerial.value = ''
    showAddMeter.value = false
    selectedMeterId.value = meter.meterAssetId
  } catch (err) {
    addError.value = problemDetail(err) ?? 'Could not add the meter. The serial may already be in use.'
  }
}

function startEdit(meter: Meter) {
  editingId.value = meter.meterAssetId
  editSerial.value = meter.serialNumber
  editSlab.value = meter.slabCode
  editError.value = null
}

async function saveEdit() {
  if (editingId.value === null) return
  editError.value = null
  try {
    await metersStore.update(editingId.value, editSerial.value, editSlab.value)
    const edited = editingId.value
    editingId.value = null
    // A new tariff changes the projected cost.
    if (edited === selectedMeterId.value) await loadForecast(edited)
  } catch (err) {
    editError.value = problemDetail(err) ?? 'Could not save the meter.'
  }
}

async function deleteMeter(meter: Meter) {
  const confirmed = window.confirm(
    `Delete meter ${meter.serialNumber}? It will no longer appear in your account.`,
  )
  if (!confirmed) return
  try {
    await metersStore.remove(meter.meterAssetId)
    if (selectedMeterId.value === meter.meterAssetId) {
      selectedMeterId.value = metersStore.meters[0]?.meterAssetId ?? null
    }
    if (auditMeterId.value === meter.meterAssetId) auditMeterId.value = null
  } catch (err) {
    window.alert(problemDetail(err) ?? 'Could not delete the meter.')
  }
}

function openAudit(meterId: number) {
  selectedMeterId.value = meterId
  auditMeterId.value = meterId
  bill.value = null
  auditError.value = null
}

async function runAudit() {
  if (!auditMeterId.value) return
  running.value = true
  auditError.value = null
  try {
    // The date inputs are Indian calendar days: send the start of the From day and
    // the end of the To day in IST, so the last day of the period is included.
    bill.value = await auditsStore.runAudit(auditMeterId.value, {
      billingCyclePeriod: period.value,
      fromDate: istDayStart(fromDate.value),
      toDate: istDayEnd(toDate.value),
    })
    await auditsStore.fetchAll()
  } catch {
    auditError.value = 'Could not run the audit. Make sure the meter has readings for this period.'
  } finally {
    running.value = false
  }
}

// ---- Billing history and projection for the selected meter ----

async function loadForecast(meterId: number) {
  forecast.value = null
  forecastError.value = null
  try {
    forecast.value = await metersStore.fetchForecast(meterId)
  } catch (err) {
    forecastError.value = problemDetail(err) ?? 'No projection available yet.'
  }
}

watch(selectedMeterId, (id) => {
  if (id) loadForecast(id)
})

const selectedMeter = computed(() => metersStore.meters.find((m) => m.meterAssetId === selectedMeterId.value))

// One bar per billing period: an audit can be re-run for a period, so keep the latest.
const history = computed<AuditReport[]>(() => {
  const latestByPeriod = new Map<string, AuditReport>()
  for (const r of auditsStore.reports) {
    if (r.meterAssetId !== selectedMeterId.value) continue
    const existing = latestByPeriod.get(r.billingCyclePeriod)
    if (!existing || r.createdOn > existing.createdOn) latestByPeriod.set(r.billingCyclePeriod, r)
  }
  return [...latestByPeriod.values()].sort((a, b) => a.billingCyclePeriod.localeCompare(b.billingCyclePeriod))
})

const historyData = computed<ChartData<'bar'>>(() => {
  const labels = history.value.map((r) => r.billingCyclePeriod)
  const cost = history.value.map((r) => r.calculatedDeterministicCost)
  const units = history.value.map((r) => r.totalUnits)
  const costColors = history.value.map(() => CHART_COLORS.primary)
  const unitColors = history.value.map(() => CHART_COLORS.secondary)

  if (forecast.value) {
    labels.push(`${forecast.value.projectedMonth} (projected)`)
    cost.push(forecast.value.projectedCost)
    units.push(forecast.value.projectedUnits)
    costColors.push(CHART_COLORS.projected)
    unitColors.push('rgba(245, 158, 11, 0.45)')
  }

  return {
    labels,
    datasets: [
      { label: 'Cost (₹)', data: cost, backgroundColor: costColors, yAxisID: 'y', borderRadius: 4 },
      { label: 'Units (kWh)', data: units, backgroundColor: unitColors, yAxisID: 'y1', borderRadius: 4 },
    ],
  }
})

const historyOptions: ChartOptions<'bar'> = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: { legend: { position: 'bottom' } },
  scales: {
    y: { beginAtZero: true, position: 'left', title: { display: true, text: '₹' } },
    y1: { beginAtZero: true, position: 'right', grid: { drawOnChartArea: false }, title: { display: true, text: 'kWh' } },
  },
}
</script>

<template>
  <div class="space-y-6">
    <header class="flex items-center justify-between">
      <div class="space-y-2">
        <h1 class="text-3xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
          Your Meters
        </h1>
        <p class="text-slate-500">Upload readings, then run an audit to see your bill.</p>
      </div>
      <button
        class="inline-flex items-center gap-2 bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-700 hover:to-cyan-700 text-white text-sm font-semibold px-4 py-2.5 rounded-lg transition-smooth"
        @click="showAddMeter = !showAddMeter"
      >
        <Plus class="w-4 h-4" /> Add meter
      </button>
    </header>

    <!-- Add meter form -->
    <section v-if="showAddMeter" class="glass glass-border rounded-xl p-6 space-y-3 max-w-lg">
      <h2 class="text-lg font-bold text-slate-900">New meter</h2>
      <input
        v-model="newSerial"
        placeholder="Serial number"
        class="w-full glass glass-border rounded-lg px-4 py-3 text-slate-800 focus:ring-2 focus:ring-blue-500/50"
      />
      <select v-model="newSlab" class="w-full glass glass-border rounded-lg px-4 py-3 text-slate-800">
        <option :value="10">Domestic Standard</option>
        <option :value="5">Domestic Lifeline</option>
      </select>
      <p v-if="addError" class="text-sm text-red-600">{{ addError }}</p>
      <button
        class="bg-blue-600 hover:bg-blue-700 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-smooth"
        @click="addMeter"
      >
        Save
      </button>
    </section>

    <!-- Meter cards -->
    <div v-if="metersStore.meters.length === 0" class="glass glass-border rounded-xl p-10 text-center text-slate-400">
      No meters yet. Add one to get started.
    </div>

    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <div
        v-for="m in metersStore.meters"
        :key="m.meterAssetId"
        :data-testid="`meter-card-${m.meterAssetId}`"
        class="glass glass-border rounded-xl p-5 space-y-3 cursor-pointer transition-smooth"
        :class="selectedMeterId === m.meterAssetId ? 'ring-2 ring-blue-500/60' : 'hover:ring-1 hover:ring-blue-300'"
        @click="selectedMeterId = m.meterAssetId"
      >
        <!-- Edit form replaces the card body while editing -->
        <template v-if="editingId === m.meterAssetId">
          <div class="space-y-2" @click.stop>
            <input
              v-model="editSerial"
              aria-label="Serial number"
              class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800"
            />
            <select v-model="editSlab" aria-label="Tariff category" class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800">
              <option :value="10">Domestic Standard</option>
              <option :value="5">Domestic Lifeline</option>
            </select>
            <p v-if="editError" class="text-sm text-red-600">{{ editError }}</p>
            <div class="flex gap-2">
              <button class="bg-blue-600 hover:bg-blue-700 text-white text-sm font-semibold px-3 py-1.5 rounded-lg" @click="saveEdit">
                Save
              </button>
              <button class="text-sm text-slate-500 hover:text-slate-800 px-3 py-1.5" @click="editingId = null">Cancel</button>
            </div>
          </div>
        </template>

        <template v-else>
          <div class="flex items-center gap-2">
            <Gauge class="w-5 h-5 text-cyan-600" />
            <span class="font-semibold text-slate-900">{{ m.serialNumber }}</span>
            <span
              v-if="m.anomalyCount > 0"
              data-testid="anomaly-badge"
              class="ml-auto inline-flex items-center gap-1 bg-red-600 text-white text-xs font-bold rounded-full px-2 py-0.5"
              :title="`${m.anomalyCount} abnormal readings`"
            >
              <TriangleAlert class="w-3 h-3" /> {{ m.anomalyCount }}
            </span>
          </div>
          <p class="text-xs text-slate-400">{{ slabLabel(m.slabCode) }}</p>
          <p class="text-sm text-slate-700">{{ m.readingCount }} readings stored</p>
          <RouterLink
            v-if="m.anomalyCount > 0"
            :to="{ name: 'anomalies', query: { meter: m.meterAssetId } }"
            class="block text-xs font-medium text-red-600 hover:underline"
            @click.stop
          >
            {{ m.anomalyCount }} abnormal readings — view anomaly log
          </RouterLink>
          <div class="flex gap-2 pt-2" @click.stop>
            <button
              class="flex-1 inline-flex items-center justify-center gap-1 bg-blue-50 text-blue-700 text-sm font-medium px-3 py-2 rounded-lg hover:bg-blue-100 transition-smooth"
              @click="openAudit(m.meterAssetId)"
            >
              <Receipt class="w-4 h-4" /> Run audit
            </button>
            <button
              class="inline-flex items-center justify-center bg-slate-100 text-slate-600 px-3 py-2 rounded-lg hover:bg-slate-200 transition-smooth"
              title="Edit meter"
              aria-label="Edit meter"
              @click="startEdit(m)"
            >
              <Pencil class="w-4 h-4" />
            </button>
            <button
              class="inline-flex items-center justify-center bg-red-50 text-red-600 px-3 py-2 rounded-lg hover:bg-red-100 transition-smooth"
              title="Delete meter"
              aria-label="Delete meter"
              @click="deleteMeter(m)"
            >
              <Trash2 class="w-4 h-4" />
            </button>
          </div>
        </template>
      </div>
    </div>

    <!-- Billing history and projection for the selected meter -->
    <section v-if="selectedMeter" class="glass glass-border rounded-xl p-6 space-y-4">
      <h2 class="text-lg font-bold text-slate-900">Bills for {{ selectedMeter.serialNumber }}</h2>

      <div v-if="history.length > 0 || forecast" class="h-64">
        <Bar :data="historyData" :options="historyOptions" />
      </div>
      <p v-else class="text-sm text-slate-400">No audits yet for this meter. Run an audit to start its history.</p>

      <div v-if="forecast" class="rounded-lg border border-amber-200 bg-amber-50 p-4 space-y-1">
        <p class="text-sm font-semibold text-amber-800">
          Projected bill for {{ forecast.projectedMonth }}: ₹{{ forecast.projectedCost }}
          <span class="font-normal">({{ forecast.projectedUnits }} kWh)</span>
        </p>
        <p class="text-xs text-amber-700">
          Projection based on your last 30 days of readings: {{ forecast.averageDailyKwh }} kWh a day
          × {{ forecast.daysInProjectedMonth }} days, priced at your current tariff
          (readings from {{ formatIst(forecast.basedOnFrom) }} to {{ formatIst(forecast.basedOnTo) }} IST).
        </p>
      </div>
      <p v-else-if="forecastError" class="text-sm text-slate-400">{{ forecastError }}</p>
    </section>

    <!-- Audit panel -->
    <section v-if="auditMeterId" class="glass glass-border rounded-xl p-6 space-y-4 max-w-2xl">
      <h2 class="text-lg font-bold text-slate-900">Run billing audit</h2>
      <div class="grid grid-cols-3 gap-3">
        <div>
          <label class="block text-xs text-slate-400 mb-1">Period</label>
          <input v-model="period" class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800" />
        </div>
        <div>
          <label class="block text-xs text-slate-400 mb-1">From</label>
          <input v-model="fromDate" type="date" class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800" />
        </div>
        <div>
          <label class="block text-xs text-slate-400 mb-1">To</label>
          <input v-model="toDate" type="date" class="w-full glass glass-border rounded-lg px-3 py-2 text-slate-800" />
        </div>
      </div>
      <button
        :disabled="running"
        class="bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-700 hover:to-cyan-700 disabled:opacity-50 text-white text-sm font-semibold px-4 py-2 rounded-lg transition-smooth"
        @click="runAudit"
      >
        {{ running ? 'Calculating…' : 'Calculate bill' }}
      </button>
      <p v-if="auditError" class="text-sm text-red-600">{{ auditError }}</p>

      <!-- Bill breakdown -->
      <div v-if="bill" class="space-y-3 pt-2 border-t border-slate-200">
        <div class="flex justify-between text-slate-800">
          <span>Total units</span><span class="font-semibold">{{ bill.totalUnits }} kWh</span>
        </div>
        <table class="w-full text-sm text-slate-700">
          <thead class="text-xs uppercase text-slate-500 border-b border-slate-200">
            <tr>
              <th class="text-left py-1">Slab</th>
              <th class="text-right py-1">Rate</th>
              <th class="text-right py-1">Units</th>
              <th class="text-right py-1">Charge</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(s, i) in bill.slabCharges" :key="i" class="border-b border-slate-100">
              <td class="py-1">{{ s.fromUnit }}–{{ s.toUnit ?? '∞' }}</td>
              <td class="text-right">₹{{ s.ratePerUnit }}</td>
              <td class="text-right">{{ s.unitsCharged }}</td>
              <td class="text-right">₹{{ s.charge }}</td>
            </tr>
          </tbody>
        </table>
        <div class="flex justify-between text-lg font-bold text-cyan-700">
          <span>Total</span><span>₹{{ bill.totalCost }}</span>
        </div>
        <button
          class="text-sm text-cyan-600 hover:underline"
          @click="router.push({ name: 'audit-detail', params: { id: bill.auditReportId } })"
        >
          View audit report & AI findings →
        </button>
      </div>
    </section>
  </div>
</template>
