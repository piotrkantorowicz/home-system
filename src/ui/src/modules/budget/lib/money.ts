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
