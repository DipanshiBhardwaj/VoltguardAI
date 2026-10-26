import { defineStore } from 'pinia'
import { ref } from 'vue'

// UI store — tiny bag of state for layout chrome. Sidebar collapse state
// is persisted to localStorage so a refresh doesn't snap it open.
const STORAGE_KEY = 'vg.sidebar.collapsed'

export const useUiStore = defineStore('ui', () => {
  const sidebarCollapsed = ref<boolean>(localStorage.getItem(STORAGE_KEY) === '1')

  function toggleSidebar() {
    sidebarCollapsed.value = !sidebarCollapsed.value
    localStorage.setItem(STORAGE_KEY, sidebarCollapsed.value ? '1' : '0')
  }

  return { sidebarCollapsed, toggleSidebar }
})
