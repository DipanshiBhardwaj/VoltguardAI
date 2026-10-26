<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import {
  LayoutDashboard,
  Upload,
  FileText,
  Settings,
  ChevronsLeft,
  ChevronsRight,
  Zap,
} from 'lucide-vue-next'
import { useUiStore } from '@/stores/ui'
import { useAuditsStore } from '@/stores/audits'

const ui = useUiStore()
const route = useRoute()
const audits = useAuditsStore()

interface NavItem {
  to: string
  label: string
  icon: typeof LayoutDashboard
  badge?: () => number
}

const items: NavItem[] = [
  { to: '/',         label: 'Dashboard',     icon: LayoutDashboard },
  { to: '/upload',   label: 'Upload Ledger', icon: Upload },
  { to: '/audits',   label: 'Audit Reports', icon: FileText, badge: () => audits.pendingCount },
  { to: '/settings', label: 'Settings',      icon: Settings },
]

// Highlight rule: exact match for '/', startsWith for the rest.
function isActive(path: string) {
  if (path === '/') return route.path === '/'
  return route.path.startsWith(path)
}

const widthClass = computed(() => ui.sidebarCollapsed ? 'w-16' : 'w-60')
</script>

<template>
  <aside
    class="flex flex-col transition-[width] duration-300 bg-white border-r border-slate-200"
    :class="widthClass"
  >
    <!-- Brand -->
    <div class="h-14 flex items-center px-4 border-b border-slate-200">
      <div class="w-7 h-7 rounded-lg bg-gradient-to-br from-blue-500 to-cyan-500 flex items-center justify-center text-white shrink-0">
        <Zap class="w-4 h-4" />
      </div>
      <span v-if="!ui.sidebarCollapsed" class="ml-3 font-semibold text-slate-800 tracking-tight">
        Voltaguard
      </span>
    </div>

    <!-- Nav -->
    <nav class="flex-1 px-2 py-4 space-y-1 overflow-y-auto">
      <RouterLink
        v-for="item in items"
        :key="item.to"
        :to="item.to"
        class="group flex items-center h-10 rounded-lg px-3 text-sm transition-smooth relative"
        :class="[
          isActive(item.to)
            ? 'bg-blue-50 text-blue-700 font-medium'
            : 'text-slate-500 hover:text-slate-800 hover:bg-slate-100',
          ui.sidebarCollapsed ? 'justify-center px-0' : ''
        ]"
        :title="ui.sidebarCollapsed ? item.label : ''"
      >
        <!-- Active indicator bar -->
        <div v-if="isActive(item.to)" class="absolute left-0 top-0 bottom-0 w-1 bg-blue-600 rounded-r" />

        <component :is="item.icon" class="w-5 h-5 shrink-0" :class="isActive(item.to) ? 'text-blue-600' : 'group-hover:text-blue-600'" />
        <span v-if="!ui.sidebarCollapsed" class="ml-3 truncate">{{ item.label }}</span>

        <!-- Badge -->
        <span
          v-if="!ui.sidebarCollapsed && item.badge && item.badge() > 0"
          class="ml-auto bg-amber-500 text-white text-xs font-bold rounded-full px-2 py-0.5 pulse-dot"
        >
          {{ item.badge() }}
        </span>
        <span
          v-else-if="ui.sidebarCollapsed && item.badge && item.badge() > 0"
          class="absolute top-0 right-0 w-2 h-2 rounded-full bg-amber-500 pulse-dot"
        />
      </RouterLink>
    </nav>

    <!-- Collapse toggle -->
    <button
      class="h-12 border-t border-slate-200 text-slate-400 hover:text-slate-700 hover:bg-slate-100 flex items-center justify-center transition-smooth"
      :title="ui.sidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'"
      @click="ui.toggleSidebar"
    >
      <ChevronsRight v-if="ui.sidebarCollapsed" class="w-5 h-5" />
      <ChevronsLeft v-else class="w-5 h-5" />
    </button>
  </aside>
</template>
