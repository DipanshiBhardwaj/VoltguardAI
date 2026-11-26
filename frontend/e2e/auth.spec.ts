import { expect, test } from '@playwright/test'
import { demoUser, mockApi } from './mockApi'

test('an unauthenticated visit redirects to /login and a successful login reaches the dashboard', async ({ page }) => {
  await mockApi(page)

  await page.goto('/')
  await expect(page).toHaveURL(/\/login/)

  await page.locator('input[type="email"]').fill(demoUser.email)
  await page.locator('input[type="password"]').fill('Customer@123')

  const loginRequest = page.waitForRequest((r) => r.url().endsWith('/api/v1/auth/login') && r.method() === 'POST')
  await page.getByRole('button', { name: 'Sign in' }).click()
  expect((await loginRequest).postDataJSON()).toEqual({ email: demoUser.email, password: 'Customer@123' })

  await expect(page).toHaveURL(/\/$/)
  await expect(page.getByRole('heading', { name: 'Your Meters' })).toBeVisible()
  await expect(page.getByTestId('meter-card-1')).toContainText('HPSEBL-DEMO-0001')
})
