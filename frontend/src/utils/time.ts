import { IST_OFFSET, IST_TIME_ZONE } from '@/api/limits'

// All timestamps from the API are UTC instants; everything the user sees is IST.

export function formatIst(iso: string, options: Intl.DateTimeFormatOptions = {}): string {
  return new Date(iso).toLocaleString('en-IN', {
    timeZone: IST_TIME_ZONE,
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    ...options,
  })
}

// Start / end of an Indian calendar day picked in a <input type="date"> (YYYY-MM-DD).
export function istDayStart(date: string): string {
  return `${date}T00:00:00.000${IST_OFFSET}`
}

export function istDayEnd(date: string): string {
  return `${date}T23:59:59.999${IST_OFFSET}`
}

// The IST calendar date (YYYY-MM-DD) of an instant.
export function istDate(iso: string): string {
  // en-CA formats as YYYY-MM-DD.
  return new Date(iso).toLocaleDateString('en-CA', { timeZone: IST_TIME_ZONE })
}
