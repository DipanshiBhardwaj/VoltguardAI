<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Gauge, Plus, Receipt } from 'lucide-vue-next'
import { useMetersStore } from '@/stores/meters'
import { useAuditsStore } from '@/stores/audits'
import type { BillSummary, SlabCode } from '@/api/types'

const router = useRouter()
const metersStore = useMetersStore()
const auditsStore = useAuditsStore()

const showAddMeter = ref(false)
const newSerial = ref('')
const newSlab = ref<SlabCode>(10)
const addError = ref<string | null>(null)

// Audit panel state, keyed by the meter it belongs to.
const auditMeterId = ref<number | null>(null)
const period = ref('2026-05')
const fromDate = ref('2026-05-01')
const toDate = ref('2026-05-31')
const running = ref(false)
const bill = ref<BillSummary | null>(null)
const auditError = ref<string | null>(null)

onMounted(() => metersStore.fetchAll())

function slabLabel(code: SlabCode) {
  return code === 5 ? 'Domestic Lifeline' : 'Domestic Standard'
}

async function addMeter() {
  addError.value = null
  try {
    await metersStore.create(newSerial.value, newSlab.value)
    newSerial.value = ''
    showAddMeter.value = false
  } catch {
    addError.value = 'Could not add the meter. The serial may already be in use.'
  }
}

function openAudit(meterId: number) {
  auditMeterId.value = meterId
  bill.value = null
  auditError.value = null
}

async function runAudit() {
  if (!auditMeterId.value) return
  running.value = true
  auditError.value = null
  try {
    bill.value = await auditsStore.runAudit(auditMeterId.value, {
      billingCyclePeriod: period.value,
      fromDate: new Date(fromDate.value).toISOString(),
      toDate: new Date(toDate.value).toISOString(),
    })
  } catch {
    auditError.value = 'Could not run the audit. Make sure the meter has readings for this period.'
  } finally {
    running.value = false
  }
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
        class="glass glass-border rounded-xl p-5 space-y-3"
      >
        <div class="flex items-center gap-2">
          <Gauge class="w-5 h-5 text-cyan-600" />
          <span class="font-semibold text-slate-900">{{ m.serialNumber }}</span>
        </div>
        <p class="text-xs text-slate-400">{{ slabLabel(m.slabCode) }}</p>
        <p class="text-sm text-slate-700">{{ m.readingCount }} readings stored</p>
        <div class="flex gap-2 pt-2">
          <button
            class="flex-1 inline-flex items-center justify-center gap-1 bg-blue-50 text-blue-700 text-sm font-medium px-3 py-2 rounded-lg hover:bg-blue-100 transition-smooth"
            @click="openAudit(m.meterAssetId)"
          >
            <Receipt class="w-4 h-4" /> Run audit
          </button>
        </div>
      </div>
    </div>

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
