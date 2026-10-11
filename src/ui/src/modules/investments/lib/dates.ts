function pad(value: number): string {
  return String(value).padStart(2, '0');
}

/** The user's local calendar day as `YYYY-MM-DD` — never via UTC, so late evenings stay on the same day. */
export function todayLocal(now: Date = new Date()): string {
  return `${String(now.getFullYear())}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

/** A `YYYY-MM-DD` date in the active language, e.g. "31 Aug 2026". */
export function formatDate(iso: string, lang: string): string {
  return new Intl.DateTimeFormat(lang, { dateStyle: 'medium', timeZone: 'UTC' }).format(
    new Date(`${iso}T00:00:00Z`),
  );
}
