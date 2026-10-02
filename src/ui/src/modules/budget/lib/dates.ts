function pad(value: number): string {
  return String(value).padStart(2, '0');
}

/** The user's local calendar day as `YYYY-MM-DD` — never via UTC, so late evenings stay on the same day. */
export function todayLocal(now: Date = new Date()): string {
  return `${String(now.getFullYear())}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

/** `iso` shifted by whole calendar days, using date parts only (no time zone involved). */
export function shiftDays(iso: string, days: number): string {
  const [year = 0, month = 1, day = 1] = iso.split('-').map(Number);
  const shifted = new Date(Date.UTC(year, month - 1, day + days));
  return shifted.toISOString().slice(0, 10);
}

export function isIsoDate(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}$/.test(value) && shiftDays(value, 0) === value;
}
