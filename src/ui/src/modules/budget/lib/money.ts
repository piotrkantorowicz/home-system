/**
 * Money stays text end to end. This only checks the shape the API accepts and swaps a decimal
 * comma for a point — it never turns the amount into a number.
 */
const AMOUNT_PATTERN = /^\d{1,10}([.,]\d{1,2})?$/;

/** The normalised API string (`"12.5"`), or `null` when `text` is not a positive amount with ≤ 2 decimals. */
export function normalizeAmount(text: string): string | null {
  const trimmed = text.trim();
  if (!AMOUNT_PATTERN.test(trimmed) || !/[1-9]/.test(trimmed)) return null;
  return trimmed.replace(',', '.');
}

/** Like `normalizeAmount` but zero is allowed — a limit of `0` is a real limit, unlike no limit. */
export function normalizeLimit(text: string): string | null {
  const trimmed = text.trim();
  return AMOUNT_PATTERN.test(trimmed) ? trimmed.replace(',', '.') : null;
}

/** An API amount (`"12.5"`, `"-40.00"`) in minor units, so amounts compare without floats. */
export function toMinor(amount: string): number {
  const negative = amount.startsWith('-');
  const [whole = '0', fraction = ''] = amount.replace('-', '').split('.');
  const minor = Number(whole) * 100 + Number(fraction.padEnd(2, '0').slice(0, 2));
  return negative ? -minor : minor;
}
