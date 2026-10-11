/** Earliest balance date the server accepts: NBP's first exchange-rate table. */
export const EARLIEST_AS_OF = '2002-01-02';

const warsawDay = new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Warsaw' });

/**
 * Today in Europe/Warsaw as `YYYY-MM-DD` — the household calendar the server checks "not in the
 * future" against, so a user in another time zone can't pick a day the server rejects.
 */
export function todayInWarsaw(now: Date = new Date()): string {
  return warsawDay.format(now);
}

/** A `YYYY-MM-DD` date in the active language, e.g. "31 Aug 2026". */
export function formatDate(iso: string, lang: string): string {
  return new Intl.DateTimeFormat(lang, { dateStyle: 'medium', timeZone: 'UTC' }).format(
    new Date(`${iso}T00:00:00Z`),
  );
}
