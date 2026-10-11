/**
 * Money stays text end to end. This only checks the shape the API accepts and swaps a decimal
 * comma for a point — it never turns the amount into a float.
 */
const AMOUNT_PATTERN = /^\d{1,10}([.,]\d{1,2})?$/;

/** The API string (`"12450.1"`) for a balance typed as `12450,1` or `12450.1`; `null` when not a valid balance. Zero is valid. */
export function normalizeAmount(text: string): string | null {
  const trimmed = text.trim().replace(/\s/g, '');
  return AMOUNT_PATTERN.test(trimmed) ? trimmed.replace(',', '.') : null;
}

/** An API amount (`"12.5"`, `"-40.00"`) in minor units, so amounts compare without floats. */
export function toMinor(amount: string): number {
  const negative = amount.startsWith('-');
  const [whole = '0', fraction = ''] = amount.replace('-', '').split('.');
  const minor = Number(whole) * 100 + Number(fraction.padEnd(2, '0').slice(0, 2));
  return negative ? -minor : minor;
}

/** Minor units as an API decimal string: `-18436` → `"-184.36"`. */
export function minorToDecimal(minor: number): string {
  const abs = Math.abs(minor);
  return `${minor < 0 ? '-' : ''}${String(Math.floor(abs / 100))}.${String(abs % 100).padStart(2, '0')}`;
}

/** The change from `previous` to `amount` as an API decimal string, or `null` for the first balance. */
export function change(amount: string, previous: string | null): string | null {
  return previous === null ? null : minorToDecimal(toMinor(amount) - toMinor(previous));
}
