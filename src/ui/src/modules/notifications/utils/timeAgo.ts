const UNITS: { unit: Intl.RelativeTimeFormatUnit; ms: number }[] = [
  { unit: 'day', ms: 86_400_000 },
  { unit: 'hour', ms: 3_600_000 },
  { unit: 'minute', ms: 60_000 },
];

export function formatTimeAgo(
  isoTimestamp: string,
  locale: string,
  now: Date = new Date(),
): string {
  const diffMs = new Date(isoTimestamp).getTime() - now.getTime();
  const absMs = Math.abs(diffMs);

  if (absMs < 60_000) return 'now';

  let formatter: Intl.RelativeTimeFormat;
  try {
    formatter = new Intl.RelativeTimeFormat(locale, { numeric: 'auto', style: 'short' });
  } catch {
    formatter = new Intl.RelativeTimeFormat('en', { numeric: 'auto', style: 'short' });
  }

  for (const { unit, ms } of UNITS) {
    if (absMs >= ms) {
      const value = Math.round(diffMs / ms);
      return formatter.format(value, unit);
    }
  }

  return 'now';
}
