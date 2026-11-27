import { expect, test, type Page } from '@playwright/test'
import { mockApi, signIn, uploadResult } from './mockApi'

const sampleCsv =
  'IntervalEnding,Voltage,ActivePower,PhaseCurrent,NeutralCurrent,PowerFactor,Frequency,CumulativeKWH\n' +
  '2026-05-01 00:00,232.0,0.52,2.41,2.39,0.93,50.01,1200.13\n'

async function uploadFile(page: Page) {
  await page.goto('/upload')
  // The meter list must have loaded before a file is accepted.
  await expect(page.locator('select option')).toHaveCount(1)
  await page.locator('input[type="file"]').setInputFiles({
    name: 'sample-ledger-2026-05.csv',
    mimeType: 'application/vnd.ms-excel',   // what Windows browsers send for .csv
    buffer: Buffer.from(sampleCsv),
  })
}

test('an upload shows accepted/rejected counts and the reason for each rejected line', async ({ page }) => {
  await signIn(page)
  await mockApi(page, {
    upload: uploadResult({
      acceptedRows: 2976,
      rejectedRows: 4,
      errors: [
        { lineNumber: 235, reason: 'Expected 8 columns but found 4.' },
        { lineNumber: 828, reason: 'Column 1 is not a valid date and time.' },
        { lineNumber: 1529, reason: 'One or more numeric columns could not be read.' },
        { lineNumber: 2338, reason: 'Voltage 999 is outside the plausible 100-300 V range.' },
      ],
    }),
  })

  await uploadFile(page)

  // The drop zone confirms which file was selected.
  await expect(page.getByTestId('selected-file')).toContainText('sample-ledger-2026-05.csv')

  await expect(page.getByTestId('accepted-count')).toHaveText('2976 rows accepted')
  await expect(page.getByTestId('rejected-count')).toHaveText('4 rows rejected')
  const rejected = page.getByTestId('rejected-lines')
  await expect(rejected.locator('li')).toHaveCount(4)
  await expect(rejected).toContainText('Line 235: Expected 8 columns but found 4.')
  await expect(rejected).toContainText('Line 2338: Voltage 999 is outside the plausible 100-300 V range.')
  await expect(page.getByRole('button', { name: 'Download error log (CSV)' })).toBeVisible()

  // No abnormal readings in this upload, so no warning.
  await expect(page.getByTestId('anomaly-banner')).toHaveCount(0)
})

test('an upload with abnormal readings shows the red anomaly banner immediately', async ({ page }) => {
  await signIn(page)
  await mockApi(page, {
    upload: uploadResult({
      acceptedRows: 2976,
      anomaliesDetected: 7,
      voltageSagCount: 4,
      currentMismatchCount: 3,
    }),
  })

  await uploadFile(page)

  const banner = page.getByTestId('anomaly-banner')
  await expect(banner).toBeVisible()
  await expect(banner).toHaveClass(/bg-red-50/)
  await expect(banner).toContainText('7 abnormal readings detected')
  await expect(banner).toContainText('4 voltage sag')
  await expect(banner).toContainText('3 phase/neutral current mismatch')
  await expect(banner).not.toContainText('voltage swell')
  await expect(banner.getByRole('link', { name: /anomaly log/i })).toHaveAttribute('href', '/anomalies?meter=1')
})
