<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { FileText, Clock, CheckCircle2 } from 'lucide-vue-next'
import { useAuditsStore } from '@/stores/audits'

const router = useRouter()
const auditsStore = useAuditsStore()

onMounted(() => auditsStore.fetchAll())

function open(id: number) {
  router.push({ name: 'audit-detail', params: { id } })
}
</script>

<template>
  <div class="space-y-6">
    <header class="space-y-2">
      <h1 class="text-3xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
        Audit Reports
      </h1>
      <p class="text-slate-500">Each billing audit, with the AI explanation once it is ready.</p>
    </header>

    <div v-if="auditsStore.reports.length === 0" class="glass glass-border rounded-xl p-10 text-center text-slate-400">
      No audit reports yet. Run an audit from the dashboard.
    </div>

    <div class="space-y-3">
      <button
        v-for="r in auditsStore.reports"
        :key="r.auditReportId"
        class="w-full glass glass-border rounded-xl p-5 flex items-center justify-between text-left hover:bg-slate-50 transition-smooth"
        @click="open(r.auditReportId)"
      >
        <div class="flex items-center gap-3">
          <FileText class="w-5 h-5 text-cyan-600" />
          <div>
            <p class="font-semibold text-slate-900">{{ r.billingCyclePeriod }}</p>
            <p class="text-sm text-slate-400">₹{{ r.calculatedDeterministicCost }}</p>
          </div>
        </div>
        <span
          v-if="r.status === 10"
          class="inline-flex items-center gap-1 text-xs text-emerald-700 bg-emerald-50 px-3 py-1 rounded-full"
        >
          <CheckCircle2 class="w-3.5 h-3.5" /> Ready
        </span>
        <span
          v-else
          class="inline-flex items-center gap-1 text-xs text-amber-600 bg-amber-50 px-3 py-1 rounded-full"
        >
          <Clock class="w-3.5 h-3.5" /> Awaiting AI
        </span>
      </button>
    </div>
  </div>
</template>
