<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { UploadCloud, FileCheck2, FileX2 } from 'lucide-vue-next'
import { useMetersStore } from '@/stores/meters'
import type { IngestionResult } from '@/api/types'

const metersStore = useMetersStore()

const selectedMeterId = ref<number | null>(null)
const isDragging = ref(false)
const uploading = ref(false)
const result = ref<IngestionResult | null>(null)
const errorMsg = ref<string | null>(null)

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
  try {
    result.value = await metersStore.upload(selectedMeterId.value, file)
  } catch {
    errorMsg.value = 'Upload failed. Check the file and try again.'
  } finally {
    uploading.value = false
  }
}

function onDrop(event: DragEvent) {
  isDragging.value = false
  const file = event.dataTransfer?.files?.[0]
  if (file) handleFile(file)
}

function onPick(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (file) handleFile(file)
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
      <input ref="fileInput" type="file" accept=".csv,.txt" class="hidden" @change="onPick" />
    </section>

    <p v-if="uploading" class="text-sm text-cyan-600">Validating rows…</p>
    <p v-if="errorMsg" class="text-sm text-red-600">{{ errorMsg }}</p>

    <!-- Result -->
    <section v-if="result" class="glass glass-border rounded-xl p-6 space-y-4">
      <div class="flex gap-6">
        <div class="flex items-center gap-2">
          <FileCheck2 class="w-5 h-5 text-emerald-400" />
          <span class="text-slate-800"><strong>{{ result.acceptedRows }}</strong> rows accepted</span>
        </div>
        <div class="flex items-center gap-2">
          <FileX2 class="w-5 h-5 text-red-400" />
          <span class="text-slate-800"><strong>{{ result.rejectedRows }}</strong> rows rejected</span>
        </div>
      </div>

      <div v-if="result.errors.length > 0" class="space-y-2">
        <h3 class="text-sm font-semibold text-slate-700">Rejected lines</h3>
        <ul class="text-sm text-slate-400 space-y-1">
          <li v-for="err in result.errors" :key="err.lineNumber">
            <span class="font-mono text-amber-600">Line {{ err.lineNumber }}:</span> {{ err.reason }}
          </li>
        </ul>
      </div>
    </section>
  </div>
</template>
