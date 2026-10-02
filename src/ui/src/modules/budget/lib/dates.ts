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

export function currentMonth(now: Date = new Date()): string {
  return todayLocal(now).slice(0, 7);
}

/** `YYYY-MM` moved by whole months; year ends and leap years need no special casing with date parts. */
export function shiftMonth(month: string, delta: number): string {
  const [year = 0, number = 1] = month.split('-').map(Number);
  const shifted = new Date(Date.UTC(year, number - 1 + delta, 1));
  return shifted.toISOString().slice(0, 7);
}

export function isMonth(value: string): boolean {
  return /^\d{4}-\d{2}$/.test(value) && shiftMonth(value, 0) === value;
}
