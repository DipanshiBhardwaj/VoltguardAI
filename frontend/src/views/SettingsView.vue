<script setup lang="ts">
import { useRouter } from 'vue-router'
import { LogOut } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()

function getInitials(name?: string) {
  if (!name) return '?'
  return name.split(' ').map(n => n[0]).join('').toUpperCase()
}

async function onLogout() {
  await auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="space-y-6 max-w-3xl">
    <!-- Header -->
    <header class="space-y-2">
      <h1 class="text-3xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
        Settings
      </h1>
      <p class="text-slate-500">Manage your account and session.</p>
    </header>

    <!-- Profile section -->
    <section class="glass glass-border rounded-xl p-6 space-y-4">
      <h2 class="text-lg font-bold text-slate-900">Profile</h2>

      <div class="flex items-start gap-4 pb-4 border-b border-slate-200">
        <!-- Avatar circle -->
        <div class="w-16 h-16 rounded-full bg-gradient-to-br from-blue-500 to-cyan-500 flex items-center justify-center text-white font-bold text-xl shrink-0">
          {{ getInitials(auth.user?.fullName) }}
        </div>

        <!-- Profile details -->
        <div class="flex-1 space-y-3">
          <div>
            <p class="text-xs uppercase text-slate-400 tracking-wider font-semibold mb-1">Full name</p>
            <p class="text-slate-900 font-semibold">{{ auth.user?.fullName ?? '—' }}</p>
          </div>
          <div>
            <p class="text-xs uppercase text-slate-400 tracking-wider font-semibold mb-1">Email address</p>
            <p class="text-slate-700 font-mono text-sm">{{ auth.user?.email ?? '—' }}</p>
          </div>
          <div>
            <p class="text-xs uppercase text-slate-400 tracking-wider font-semibold mb-1">Role</p>
            <span class="text-xs bg-blue-50 text-blue-700 px-3 py-1.5 rounded-full font-semibold">
              {{ auth.user?.role ?? '—' }}
            </span>
          </div>
        </div>
      </div>

      <p class="text-xs text-slate-400">
        Account profile and authentication managed by Voltaguard-AI.
      </p>
    </section>

    <!-- Session section -->
    <section class="glass glass-border rounded-xl p-6 space-y-4">
      <h2 class="text-lg font-bold text-slate-900">Session & Security</h2>

      <div class="space-y-3">
        <p class="text-sm text-slate-400">
          You are logged in to Voltaguard-AI as <span class="text-cyan-600 font-semibold">{{ auth.user?.fullName }}</span>.
          Sign out to end your session securely.
        </p>

        <button
          class="inline-flex items-center gap-2 bg-gradient-to-r from-red-600 to-red-700 hover:from-red-700 hover:to-red-800 text-white text-sm font-semibold px-5 py-2.5 rounded-lg transition-all duration-300 hover:shadow-lg hover:shadow-red-500/50 active:scale-95"
          @click="onLogout"
        >
          <LogOut class="w-4 h-4" />
          <span>Sign out</span>
        </button>
      </div>
    </section>

    <!-- Info section -->
    <section class="glass glass-border rounded-xl p-6">
      <h2 class="text-lg font-bold text-slate-900 mb-4">About Voltaguard-AI</h2>
      <div class="space-y-2 text-sm text-slate-400">
        <p>
          <span class="text-slate-800 font-semibold">Version:</span> 1.0.0-alpha
        </p>
        <p>
          <span class="text-slate-800 font-semibold">Environment:</span> <code class="bg-slate-100 px-2 py-0.5 rounded text-amber-700">Development</code>
        </p>
        <p>
          <span class="text-slate-800 font-semibold">Architecture:</span> Vue 3 + .NET 9 Web API + Python LangGraph AI Agent
        </p>
      </div>
    </section>
  </div>
</template>
