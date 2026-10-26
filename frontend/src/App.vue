<script setup lang="ts">
import { computed } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import AppSidebar from '@/components/AppSidebar.vue'
import AppTopbar from '@/components/AppTopbar.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const route = useRoute()

const showShell = computed(() => Boolean(auth.user) && !route.meta.public)
</script>

<template>
  <!-- Animated background grid (visible on all pages) -->
  <div class="animated-grid-bg" />

  <!-- Main app container -->
  <div class="relative z-10">
    <!-- Authenticated shell: sidebar + topbar + main content -->
    <div v-if="showShell" class="flex h-screen overflow-hidden">
      <AppSidebar />
      <div class="flex-1 flex flex-col overflow-hidden bg-slate-50">
        <AppTopbar />
        <main class="flex-1 overflow-y-auto p-6">
          <!-- Page transition wrapper -->
          <Transition name="slide-fade" mode="out-in">
            <RouterView :key="route.fullPath" />
          </Transition>
        </main>
      </div>
    </div>

    <!-- Public pages (Login) - full-bleed -->
    <Transition name="slide-fade" mode="out-in">
      <RouterView v-if="!showShell" :key="route.fullPath" />
    </Transition>
  </div>
</template>

<style scoped>
.slide-fade-enter-active,
.slide-fade-leave-active {
  transition: all 0.3s ease;
}

.slide-fade-enter-from {
  transform: translateX(10px);
  opacity: 0;
}

.slide-fade-leave-to {
  transform: translateX(-10px);
  opacity: 0;
}
</style>
