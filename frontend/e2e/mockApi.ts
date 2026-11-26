import type { Page, Route } from '@playwright/test'
import type { CurrentUser, IngestionResult, Meter } from '../src/api/types'

// Stand-in for the .NET API: every request to /api/** is answered here, so the
// tests run without the backend or a database.

export const TOKEN_KEY = 'voltaguard.token'

export const demoUser: CurrentUser = {
  userId: 2,
  email: 'customer@voltaguard.local',
  fullName: 'Demo Customer',
  role: 'Customer',
  token: 'e2e-test-token',
}

export function meter(overrides: Partial<Meter> = {}): Meter {
  return {
    meterAssetId: 1,
    serialNumber: 'HPSEBL-DEMO-0001',
    slabCode: 10,
    readingCount: 0,
    latestReadingOn: null,
    anomalyCount: 0,
    ...overrides,
  }
}

export function uploadResult(overrides: Partial<IngestionResult> = {}): IngestionResult {
  return {
    acceptedRows: 0,
    rejectedRows: 0,
    errors: [],
    anomaliesDetected: 0,
    voltageSagCount: 0,
    voltageSwellCount: 0,
    currentMismatchCount: 0,
    ...overrides,
  }
}

interface MockOptions {
  meters?: Meter[]
  upload?: IngestionResult
}

function json(route: Route, body: unknown, status = 200) {
  return route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) })
}

function problem(route: Route, status: number, detail: string) {
  return route.fulfill({
    status,
    contentType: 'application/problem+json',
    body: JSON.stringify({ status, title: 'Request could not be processed', detail }),
  })
}

export async function mockApi(page: Page, options: MockOptions = {}) {
  const meters = options.meters ?? [meter()]

  // Match on the path, not a glob: Vite also serves source files such as
  // /src/api/client.ts, which a "**/api/**" glob would wrongly catch.
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    async (route) => {
      const request = route.request()
      const method = request.method()
      const path = new URL(request.url()).pathname

      if (method === 'POST' && path === '/api/v1/auth/login') return json(route, demoUser)
      if (method === 'GET' && path === '/api/v1/auth/me') {
        return request.headers().authorization ? json(route, demoUser) : route.fulfill({ status: 401 })
      }
      if (method === 'GET' && path === '/api/v1/meters') return json(route, meters)
      if (method === 'GET' && path === '/api/v1/audit-reports') return json(route, [])
      if (method === 'GET' && /^\/api\/v1\/meters\/\d+\/forecast$/.test(path)) {
        return problem(route, 400, 'This meter has no readings to project a bill from.')
      }
      if (method === 'POST' && /^\/api\/v1\/meters\/\d+\/telemetry$/.test(path)) {
        return json(route, options.upload ?? uploadResult())
      }

      return problem(route, 404, `No mock for ${method} ${path}`)
    },
  )
}

// Starts the page already signed in (a stored token that /auth/me accepts).
export async function signIn(page: Page) {
  // Passed as script text: it runs in the browser, which this Node-typed file cannot see.
  await page.addInitScript({
    content: `localStorage.setItem(${JSON.stringify(TOKEN_KEY)}, ${JSON.stringify(demoUser.token)});`,
  })
}
