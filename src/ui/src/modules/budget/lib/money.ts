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

/** Minor units as an API decimal string: `18436` → `"184.36"`. */
export function minorToDecimal(minor: number): string {
  const abs = Math.abs(minor);
  return `${minor < 0 ? '-' : ''}${String(Math.floor(abs / 100))}.${String(abs % 100).padStart(2, '0')}`;
}

/**
 * The backend's equal split, exactly: remainder cents go one each to the first participants in
 * ascending person-id order (so 100.00 over three people is 33.34 / 33.33 / 33.33), never in the
 * order they were ticked. Ids are GUIDs; .NET orders them field by field, which for the canonical
 * lower-case text form is the same as comparing the text.
 */
export function splitEqual(totalMinor: number, personIds: readonly string[]): Map<string, number> {
  const ordered = [...new Set(personIds)].sort((a, b) => {
    const left = a.toLowerCase();
    const right = b.toLowerCase();
    return left < right ? -1 : left > right ? 1 : 0;
  });
  const shares = new Map<string, number>();
  if (ordered.length === 0) return shares;
  const each = Math.floor(totalMinor / ordered.length);
  const remainder = totalMinor - each * ordered.length;
  ordered.forEach((id, index) => {
    shares.set(id, each + (index < remainder ? 1 : 0));
  });
  return shares;
}
