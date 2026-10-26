import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/login',          name: 'login',        component: () => import('@/views/LoginView.vue'),       meta: { public: true } },
    { path: '/',               name: 'dashboard',    component: () => import('@/views/DashboardView.vue') },
    { path: '/upload',         name: 'upload',       component: () => import('@/views/UploadView.vue') },
    { path: '/audits',         name: 'audits',       component: () => import('@/views/AuditsView.vue') },
    { path: '/audits/:id',     name: 'audit-detail', component: () => import('@/views/AuditDetailView.vue'), props: true },
    { path: '/settings',       name: 'settings',     component: () => import('@/views/SettingsView.vue') },
  ],
})

// Wait for the first session probe before deciding where to send the user.
router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!auth.isReady) await auth.probeSession()
  if (to.meta.public) return true
  if (!auth.user) return { name: 'login', query: { redirect: to.fullPath } }
  return true
})

export default router
