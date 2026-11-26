<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { ArrowLeft, Clock } from 'lucide-vue-next'
import { Bar } from 'vue-chartjs'
import type { ChartData, ChartOptions } from 'chart.js'
import { CHART_COLORS } from '@/charts'
import { formatIst } from '@/utils/time'
import { useAuditsStore } from '@/stores/audits'
import type { AuditReport } from '@/api/types'

const props = defineProps<{ id: string }>()
const router = useRouter()
const auditsStore = useAuditsStore()

const report = ref<AuditReport | null>(null)
const loading = ref(true)

onMounted(async () => {
  report.value = await auditsStore.fetchOne(Number(props.id))
  loading.value = false
})

function hourLabel(hour: number) {
  const h = hour % 12 === 0 ? 12 : hour % 12
  return `${h}:00 ${hour < 12 ? 'AM' : 'PM'}`
}

// The four time-of-day bands (IST clock hours, as in ConsumptionAnalyzer).
const bandData = computed<ChartData<'bar'>>(() => {
  const f = report.value?.facts
  return {
    labels: ['Morning (6–12)', 'Daytime (12–17)', 'Evening (17–23)', 'Night (23–6)'],
    datasets: [
      {
        label: 'kWh',
        data: f ? [f.morningKwh, f.dayKwh, f.eveningKwh, f.nightKwh] : [],
        backgroundColor: CHART_COLORS.primary,
        borderRadius: 6,
      },
    ],
  }
})

const bandOptions: ChartOptions<'bar'> = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: { callbacks: { label: (ctx) => `${ctx.parsed.y} kWh` } },
  },
  scales: { y: { beginAtZero: true, title: { display: true, text: 'kWh' } } },
}

// AI findings are untrusted text, so sanitise the rendered HTML before showing it.
const findingsHtml = computed(() => {
  if (!report.value?.aiAnalysisFindings) return ''
  const raw = marked.parse(report.value.aiAnalysisFindings, { async: false }) as string
  return DOMPurify.sanitize(raw)
})
</script>

<template>
  <div class="space-y-6 max-w-3xl">
    <button class="inline-flex items-center gap-1 text-sm text-slate-400 hover:text-cyan-600" @click="router.back()">
      <ArrowLeft class="w-4 h-4" /> Back
    </button>

    <div v-if="loading" class="text-slate-400">Loading…</div>

    <template v-else-if="report">
      <header class="space-y-2">
        <h1 class="text-3xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
          Audit — {{ report.billingCyclePeriod }}
        </h1>
        <p class="text-2xl font-semibold text-slate-900">₹{{ report.calculatedDeterministicCost }}</p>
      </header>

      <!-- Measured facts (computed from the readings, not guessed) -->
      <section class="glass glass-border rounded-xl p-6 space-y-5">
        <h2 class="text-lg font-bold text-slate-900">What the meter data shows</h2>

        <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
          <div>
            <p class="text-xs uppercase tracking-wider text-slate-400">Total used</p>
            <p class="text-xl font-semibold text-slate-900">{{ report.totalUnits }} <span class="text-sm font-normal text-slate-500">kWh</span></p>
          </div>
          <div>
            <p class="text-xs uppercase tracking-wider text-slate-400">Avg per day</p>
            <p class="text-xl font-semibold text-slate-900">{{ report.facts.averageDailyKwh }} <span class="text-sm font-normal text-slate-500">kWh</span></p>
          </div>
          <div>
            <p class="text-xs uppercase tracking-wider text-slate-400">Busiest hour</p>
            <p class="text-xl font-semibold text-slate-900">{{ hourLabel(report.facts.peakHour) }}</p>
          </div>
          <div>
            <p class="text-xs uppercase tracking-wider text-slate-400">Highest draw</p>
            <p class="text-xl font-semibold text-slate-900">{{ report.facts.highestDemandKW }} <span class="text-sm font-normal text-slate-500">kW</span></p>
          </div>
        </div>

        <!-- Time-of-day split -->
        <div class="space-y-2">
          <p class="text-sm font-semibold text-slate-700">When the energy was used (IST)</p>
          <div class="h-56">
            <Bar :data="bandData" :options="bandOptions" />
          </div>
        </div>

        <p v-if="report.facts.highestDemandOn" class="text-xs text-slate-400">
          Highest single draw of {{ report.facts.highestDemandKW }} kW recorded on
          {{ formatIst(report.facts.highestDemandOn) }} IST.
        </p>
      </section>

      <!-- AI findings -->
      <section class="glass glass-border rounded-xl p-6">
        <h2 class="text-lg font-bold text-slate-900 mb-4">Energy auditor's notes</h2>

        <div v-if="report.status === 10" class="prose max-w-none" v-html="findingsHtml" />

        <div v-else class="flex items-center gap-2 text-amber-600">
          <Clock class="w-5 h-5" />
          <span>The local AI agent is still preparing this explanation. Check back shortly.</span>
        </div>
      </section>
    </template>
  </div>
</template>
