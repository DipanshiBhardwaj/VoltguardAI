<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { isAxiosError } from 'axios'
import { UploadCloud, FileCheck2, FileX2, TriangleAlert, Download } from 'lucide-vue-next'
import { useMetersStore } from '@/stores/meters'
import type { IngestionResult } from '@/api/types'
import { NORMAL_MAX_VOLTAGE, NORMAL_MIN_VOLTAGE } from '@/api/limits'

const metersStore = useMetersStore()

const selectedMeterId = ref<number | null>(null)
const isDragging = ref(false)
const uploading = ref(false)
const result = ref<IngestionResult | null>(null)
const errorMsg = ref<string | null>(null)
const lastFileName = ref('')

function problemDetail(err: unknown): string | null {
  if (isAxiosError(err) && typeof err.response?.data?.detail === 'string') {
    return err.response.data.detail
  }
  return null
}

onMounted(async () => {
  await metersStore.fetchAll()
  const first = metersStore.meters[0]
  if (first) {
    selectedMeterId.value = first.meterAssetId
  }
})

async function handleFile(file: File) {
  if (!selectedMeterId.value) {
    errorMsg.value = 'Select a meter first.'
    return
  }

  uploading.value = true
  errorMsg.value = null
  result.value = null
  lastFileName.value = file.name
  try {
    result.value = await metersStore.upload(selectedMeterId.value, file)
  } catch (err) {
    // The API explains a rejected file (wrong type, binary content...) in ProblemDetails.
    errorMsg.value = problemDetail(err) ?? 'Upload failed. Check the file and try again.'
  } finally {
    uploading.value = false
  }
}

// Rejected lines of the last upload as a CSV the user can open next to their file.
function downloadErrorLog() {
  if (!result.value) return
  const quote = (s: string) => `"${s.replace(/"/g, '""')}"`
  const lines = ['LineNumber,Reason', ...result.value.errors.map((e) => `${e.lineNumber},${quote(e.reason)}`)]
  const blob = new Blob([lines.join('\r\n') + '\r\n'], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `${lastFileName.value.replace(/\.[^.]+$/, '') || 'ledger'}-error-log.csv`
  link.click()
  URL.revokeObjectURL(url)
}

function onDrop(event: DragEvent) {
  isDragging.value = false
  const file = event.dataTransfer?.files?.[0]
  if (file) handleFile(file)
}

function onPick(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (file) handleFile(file)
  // Reset so picking the same file again still fires @change.
  input.value = ''
}
</script>

<template>
  <div class="space-y-6 max-w-3xl">
    <header class="space-y-2">
      <h1 class="text-3xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
        Upload Usage Ledger
      </h1>
      <p class="text-slate-500">
        Export your usage ledger from the utility portal and drop the CSV here. Each row is
        checked before it is saved.
      </p>
    </header>

    <!-- Meter picker -->
    <section class="glass glass-border rounded-xl p-6 space-y-3">
      <label class="block text-sm font-semibold text-slate-700">Meter</label>
      <select
        v-model="selectedMeterId"
        class="w-full glass glass-border rounded-lg px-4 py-3 text-slate-800 focus:ring-2 focus:ring-blue-500/50"
      >
        <option v-for="m in metersStore.meters" :key="m.meterAssetId" :value="m.meterAssetId">
          {{ m.serialNumber }}
        </option>
      </select>
      <p v-if="metersStore.meters.length === 0" class="text-sm text-amber-600">
        No meters yet. Add one from the dashboard first.
      </p>
    </section>

    <!-- Drop zone -->
    <section
      class="glass glass-border rounded-xl p-10 text-center transition-smooth cursor-pointer"
      :class="isDragging ? 'ring-2 ring-blue-500/60 bg-blue-500/10' : ''"
      @dragover.prevent="isDragging = true"
      @dragleave.prevent="isDragging = false"
      @drop.prevent="onDrop"
      @click="($refs.fileInput as HTMLInputElement).click()"
    >
      <UploadCloud class="w-12 h-12 mx-auto text-blue-400 mb-3" />
      <p class="text-slate-800 font-medium">Drag a CSV file here, or click to choose</p>
      <p class="text-xs text-slate-500 mt-1">Comma-separated, up to 10 MB</p>
      <p v-if="lastFileName" data-testid="selected-file" class="text-sm text-slate-700 mt-3">
        <FileCheck2 class="w-4 h-4 inline-block -mt-0.5 text-emerald-500" />
        Selected: <span class="font-mono">{{ lastFileName }}</span>
      </p>
      <input ref="fileInput" type="file" accept=".csv,.txt" class="hidden" @change="onPick" />
    </section>

    <p v-if="uploading" class="text-sm text-cyan-600">Validating rows…</p>
    <p v-if="errorMsg" class="text-sm text-red-600">{{ errorMsg }}</p>

    <!-- Anomaly warning: shown as soon as the upload response arrives -->
    <section
      v-if="result && result.anomaliesDetected > 0"
      data-testid="anomaly-banner"
      role="alert"
      class="rounded-xl border border-red-300 bg-red-50 p-5 space-y-3"
    >
      <div class="flex items-center gap-2 text-red-700">
        <TriangleAlert class="w-5 h-5" />
        <h2 class="font-bold">{{ result.anomaliesDetected }} abnormal readings detected</h2>
      </div>
      <ul class="text-sm text-red-700 space-y-1">
        <li v-if="result.voltageSagCount > 0">
          <strong>{{ result.voltageSagCount }}</strong> voltage sag (below {{ NORMAL_MIN_VOLTAGE }} V)
        </li>
        <li v-if="result.voltageSwellCount > 0">
          <strong>{{ result.voltageSwellCount }}</strong> voltage swell (above {{ NORMAL_MAX_VOLTAGE }} V)
        </li>
        <li v-if="result.currentMismatchCount > 0">
          <strong>{{ result.currentMismatchCount }}</strong> phase/neutral current mismatch (possible tampering or earth leakage)
        </li>
      </ul>
      <RouterLink
        :to="{ name: 'anomalies', query: { meter: selectedMeterId } }"
        class="inline-block text-sm font-semibold text-red-700 underline hover:text-red-800"
      >
        Open the anomaly log →
      </RouterLink>
    </section>

    <!-- Result -->
    <section v-if="result" data-testid="upload-result" class="glass glass-border rounded-xl p-6 space-y-4">
      <div class="flex flex-wrap gap-6">
        <div class="flex items-center gap-2">
          <FileCheck2 class="w-5 h-5 text-emerald-400" />
          <span class="text-slate-800" data-testid="accepted-count"><strong>{{ result.acceptedRows }}</strong> rows accepted</span>
        </div>
        <div class="flex items-center gap-2">
          <FileX2 class="w-5 h-5 text-red-400" />
          <span class="text-slate-800" data-testid="rejected-count"><strong>{{ result.rejectedRows }}</strong> rows rejected</span>
        </div>
      </div>

      <div v-if="result.errors.length > 0" class="space-y-2">
        <div class="flex items-center justify-between gap-3">
          <h3 class="text-sm font-semibold text-slate-700">Rejected lines</h3>
          <button
            type="button"
            class="inline-flex items-center gap-1 text-sm font-medium text-blue-700 bg-blue-50 hover:bg-blue-100 px-3 py-1.5 rounded-lg transition-smooth"
            @click="downloadErrorLog"
          >
            <Download class="w-4 h-4" /> Download error log (CSV)
          </button>
        </div>
        <ul class="text-sm text-slate-400 space-y-1" data-testid="rejected-lines">
          <li v-for="err in result.errors" :key="err.lineNumber">
            <span class="font-mono text-amber-600">Line {{ err.lineNumber }}:</span> {{ err.reason }}
          </li>
        </ul>
      </div>
    </section>
  </div>
</template>
