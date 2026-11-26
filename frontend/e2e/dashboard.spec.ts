import { expect, test } from '@playwright/test'
import { meter, mockApi, signIn } from './mockApi'

test('a meter with abnormal readings shows a red badge on its card', async ({ page }) => {
  await signIn(page)
  await mockApi(page, {
    meters: [
      meter({ meterAssetId: 1, serialNumber: 'HPSEBL-DEMO-0001', readingCount: 2976, anomalyCount: 7 }),
      meter({ meterAssetId: 2, serialNumber: 'HPSEBL-CLEAN-0002', readingCount: 96, anomalyCount: 0 }),
    ],
  })

  await page.goto('/')

  const flagged = page.getByTestId('meter-card-1')
  await expect(flagged).toContainText('2976 readings stored')
  const badge = flagged.getByTestId('anomaly-badge')
  await expect(badge).toBeVisible()
  await expect(badge).toHaveText('7')
  await expect(badge).toHaveClass(/bg-red-600/)

  // A meter without anomalies has no badge.
  await expect(page.getByTestId('meter-card-2')).toBeVisible()
  await expect(page.getByTestId('meter-card-2').getByTestId('anomaly-badge')).toHaveCount(0)
})
