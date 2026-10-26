<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Zap } from 'lucide-vue-next'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('customer@voltaguard.local')
const password = ref('Customer@123')
const fullName = ref('')
const isRegister = ref(false)
const submitting = ref(false)
const errorMsg = ref<string | null>(null)

function toggleMode() {
  isRegister.value = !isRegister.value
  errorMsg.value = null
}

async function onSubmit() {
  submitting.value = true
  errorMsg.value = null
  try {
    if (isRegister.value) {
      await auth.register(email.value, password.value, fullName.value)
    } else {
      await auth.login(email.value, password.value)
    }
    const redirect = (route.query.redirect as string) || '/'
    router.replace(redirect)
  } catch {
    errorMsg.value = isRegister.value
      ? 'Could not create the account. Try a different email.'
      : 'Invalid email or password.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="relative min-h-screen overflow-hidden bg-gradient-to-br from-slate-50 via-blue-50 to-cyan-50">
    <!-- Faint grid background -->
    <div class="absolute inset-0 bg-[linear-gradient(0deg,rgba(37,99,235,0.04)_1px,transparent_1px),linear-gradient(90deg,rgba(37,99,235,0.04)_1px,transparent_1px)] bg-[48px_48px]" />

    <!-- Floating gradient orbs -->
    <div class="absolute -top-40 -left-40 w-80 h-80 bg-blue-300/30 rounded-full blur-3xl" />
    <div class="absolute -bottom-40 -right-40 w-80 h-80 bg-cyan-300/30 rounded-full blur-3xl" />

    <!-- Main content -->
    <div class="relative z-10 flex items-center justify-center min-h-screen p-4">
      <form
        class="glass glass-border rounded-2xl w-full max-w-sm p-8 space-y-6 hover:glow-primary"
        @submit.prevent="onSubmit"
      >
        <!-- Logo -->
        <div class="flex items-center gap-3 mb-6">
          <div class="w-10 h-10 rounded-lg bg-gradient-to-br from-blue-500 to-cyan-500 flex items-center justify-center animate-pulse">
            <Zap class="w-6 h-6 text-white" />
          </div>
          <h1 class="text-3xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent">
            Voltaguard-AI
          </h1>
        </div>

        <p class="text-sm text-slate-400">
          {{ isRegister ? 'Create an account to analyse your energy usage.' : 'Sign in to analyse your energy usage.' }}
        </p>

        <!-- Full name (register only) -->
        <div v-if="isRegister" class="space-y-2">
          <label class="block text-sm font-semibold text-slate-700">Full name</label>
          <input
            v-model="fullName"
            type="text"
            required
            autocomplete="name"
            class="w-full glass glass-border rounded-lg px-4 py-3 text-slate-800 focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 transition-smooth"
            placeholder="Your name"
          />
        </div>

        <!-- Email input -->
        <div class="space-y-2">
          <label class="block text-sm font-semibold text-slate-700">Email</label>
          <input
            v-model="email"
            type="email"
            required
            autocomplete="email"
            class="w-full glass glass-border rounded-lg px-4 py-3 text-slate-800 focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 transition-smooth"
            placeholder="admin@voltaguard.local"
          />
        </div>

        <!-- Password input -->
        <div class="space-y-2">
          <label class="block text-sm font-semibold text-slate-700">Password</label>
          <input
            v-model="password"
            type="password"
            required
            autocomplete="current-password"
            class="w-full glass glass-border rounded-lg px-4 py-3 text-slate-800 focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 transition-smooth"
            placeholder="••••••••"
          />
        </div>

        <!-- Error message -->
        <Transition name="slide-down">
          <div v-if="errorMsg" class="p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">
            {{ errorMsg }}
          </div>
        </Transition>

        <!-- Submit button -->
        <button
          type="submit"
          :disabled="submitting"
          class="w-full bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-700 hover:to-cyan-700 disabled:opacity-50 disabled:cursor-not-allowed text-white font-semibold py-3 rounded-lg transition-smooth active:scale-95 flex items-center justify-center gap-2"
        >
          <Zap v-if="!submitting" class="w-4 h-4" />
          {{ submitting ? 'Please wait…' : (isRegister ? 'Create account' : 'Sign in') }}
        </button>

        <!-- Mode toggle -->
        <p class="text-xs text-slate-500 text-center">
          {{ isRegister ? 'Already have an account?' : 'New here?' }}
          <button type="button" class="text-cyan-600 hover:underline" @click="toggleMode">
            {{ isRegister ? 'Sign in' : 'Create one' }}
          </button>
        </p>

        <!-- Footer text -->
        <p v-if="!isRegister" class="text-xs text-slate-500 text-center">
          Demo: <code class="text-slate-400">customer@voltaguard.local</code> / <code class="text-slate-400">Customer@123</code>
        </p>
      </form>
    </div>
  </div>
</template>

<style scoped>
@keyframes slide-down {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.slide-down-enter-active {
  animation: slide-down 0.3s ease-out;
}

.slide-down-leave-active {
  animation: slide-down 0.3s ease-in reverse;
}
</style>
