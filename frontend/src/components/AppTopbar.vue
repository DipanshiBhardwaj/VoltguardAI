<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { LogOut, UserCircle2 } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const currentTime = ref<string>('')
let timer: ReturnType<typeof setInterval> | null = null

function updateTime() {
  const now = new Date()
  currentTime.value = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

onMounted(() => {
  updateTime()
  timer = setInterval(updateTime, 1000)
})

onBeforeUnmount(() => {
  if (timer) clearInterval(timer)
})

async function onLogout() {
  await auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <header class="h-14 flex items-center justify-between px-6 bg-white border-b border-slate-200">
    <!-- Left: Live clock -->
    <div class="flex items-center gap-3">
      <div class="w-2 h-2 rounded-full bg-emerald-500 pulse-dot" title="System online" />
      <span class="text-xs font-mono text-slate-500">{{ currentTime }}</span>
    </div>

    <!-- Right: User profile + logout -->
    <div class="flex items-center gap-4">
      <div class="flex items-center gap-2 text-sm">
        <UserCircle2 class="w-5 h-5 text-cyan-600" />
        <span class="text-slate-700">{{ auth.user?.fullName }}</span>
        <span class="text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded-full">
          {{ auth.user?.role }}
        </span>
      </div>
      <button
        class="text-slate-400 hover:text-red-500 transition-smooth hover:scale-110"
        title="Sign out"
        @click="onLogout"
      >
        <LogOut class="w-5 h-5" />
      </button>
    </div>
  </header>
</template>
