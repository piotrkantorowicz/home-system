/**
 * One way to write every number, unit and date (redesign v3 spec §1.4).
 * Pure functions — pass the language and the user's preferences in; `useFormat()` binds them.
 *
 * Stored values stay canonical (kcal, kg, ml, decimal-string money). Unit preferences only
 * change what is *displayed*, and every converted value carries its unit label, so a value is
 * never silently relabelled.
 */

export type Lang = 'en' | 'pl' | (string & {});

export interface FormatPrefs {
  lang: Lang;
  thinSpaceThousands: boolean;
  energyUnit?: 'kcal' | 'kJ';
  weightUnit?: 'kg' | 'lb';
  volumeUnit?: 'ml' | 'L' | 'oz';
}

/** Shown for missing or non-finite input instead of "NaN" / "undefined". */
export const NO_VALUE = '–';

const NNBSP = ' '; // narrow no-break space: never wraps inside a number
const MINUS = '−';
const KJ_PER_KCAL = 4.184;
const LB_PER_KG = 2.2046226218;
const ML_PER_FL_OZ = 29.5735;

// Newer ICU writes en-GB September as "Sept"; the spec (and older engines) use "Sep".
const sep3 = (text: string) => text.replace(/\bSept\b/, 'Sep');

const isPl = (lang: Lang) => lang.startsWith('pl');
const localeFor = (lang: Lang) => (isPl(lang) ? 'pl-PL' : 'en-GB');
const finite = (v: number | null | undefined): v is number =>
  typeof v === 'number' && Number.isFinite(v);

function sepFor(prefs: FormatPrefs): string {
  return isPl(prefs.lang) || prefs.thinSpaceThousands ? NNBSP : ',';
}

function group(intDigits: string, prefs: FormatPrefs): string {
  return intDigits.replace(/\B(?=(\d{3})+(?!\d))/g, sepFor(prefs));
}

/**
 * Grouping is done by hand: Intl's pl-PL leaves 4-digit numbers ungrouped ("4114,65"),
 * which would make columns inconsistent.
 */
function grouped(value: number, prefs: FormatPrefs, min: number, max: number): string {
  const parts = new Intl.NumberFormat(localeFor(prefs.lang), {
    minimumFractionDigits: min,
    maximumFractionDigits: max,
    useGrouping: false,
  }).formatToParts(Math.abs(value));
  const int = parts
    .filter((p) => p.type === 'integer')
    .map((p) => p.value)
    .join('');
  const decimal = parts.find((p) => p.type === 'decimal')?.value ?? '';
  const frac = parts
    .filter((p) => p.type === 'fraction')
    .map((p) => p.value)
    .join('');
  const nonZero = parts.some(
    (p) => (p.type === 'integer' || p.type === 'fraction') && /[1-9]/.test(p.value),
  );
  return (value < 0 && nonZero ? MINUS : '') + group(int, prefs) + (frac ? decimal + frac : '');
}

/** Energy in the preferred unit with its label: 2100 → "2 100 kcal" / "8 786 kJ". */
export function formatEnergy(kcal: number | null | undefined, prefs: FormatPrefs): string {
  if (!finite(kcal)) return NO_VALUE;
  if (prefs.energyUnit === 'kJ')
    return `${grouped(Math.round(kcal * KJ_PER_KCAL), prefs, 0, 0)} kJ`;
  return `${grouped(Math.round(kcal), prefs, 0, 0)} kcal`;
}

/** Grams of a macro: one decimal only under 10. 46.0 → "46", 4.54 → "4.5", 0 → "0". */
export function formatGrams(g: number | null | undefined, prefs: FormatPrefs): string {
  if (!finite(g)) return NO_VALUE;
  if (g === 0) return '0';
  if (Math.abs(g) < 10) {
    const rounded = Math.round(g * 10) / 10;
    return grouped(rounded, prefs, Number.isInteger(rounded) ? 0 : 1, 1);
  }
  return grouped(Math.round(g), prefs, 0, 0);
}

/** Body weight in the preferred unit: 72.46 kg → "72.5 kg" / "159.7 lb". */
export function formatWeight(kg: number | null | undefined, prefs: FormatPrefs): string {
  if (!finite(kg)) return NO_VALUE;
  if (prefs.weightUnit === 'lb')
    return `${grouped(Math.round(kg * LB_PER_KG * 10) / 10, prefs, 0, 1)} lb`;
  return `${grouped(Math.round(kg * 10) / 10, prefs, 0, 1)} kg`;
}

/** A single drink amount in the preferred volume unit: 250 → "250 ml" / "0.3 L" / "8 oz". */
export function formatVolume(ml: number | null | undefined, prefs: FormatPrefs): string {
  if (!finite(ml)) return NO_VALUE;
  if (prefs.volumeUnit === 'oz') return `${grouped(Math.round(ml / ML_PER_FL_OZ), prefs, 0, 0)} oz`;
  if (prefs.volumeUnit === 'L') return `${grouped(Math.round(ml / 10) / 100, prefs, 0, 2)} L`;
  return `${grouped(Math.round(ml), prefs, 0, 0)} ml`;
}

/** Litres with one decimal: 1330 → "1.3". */
export function formatLitres(ml: number | null | undefined, prefs: FormatPrefs): string {
  if (!finite(ml)) return NO_VALUE;
  return grouped(Math.round(ml / 100) / 10, prefs, 1, 1);
}

/** "1.3 of 2.5 L" — the one way water progress is written (fluid ounces when that is preferred). */
export function formatWaterProgress(
  ml: number | null | undefined,
  goalMl: number | null | undefined,
  prefs: FormatPrefs,
  ofWord = 'of',
): string {
  if (!finite(ml) || !finite(goalMl)) return NO_VALUE;
  if (prefs.volumeUnit === 'oz') {
    const oz = (v: number) => grouped(Math.round(v / ML_PER_FL_OZ), prefs, 0, 0);
    return `${oz(ml)} ${ofWord} ${oz(goalMl)} oz`;
  }
  return `${formatLitres(ml, prefs)} ${ofWord} ${formatLitres(goalMl, prefs)} L`;
}

// ---- Money: decimal strings in, integer minor units for any arithmetic. Never floats. ----

/**
 * Parse an API decimal ("184.36", "-0.5") to integer minor units (cents), rounding half away
 * from zero at the third decimal. Returns null for anything that is not a plain decimal.
 * A JS number is accepted for convenience but only its 2-decimal rendering is trusted.
 */
export function toMinorUnits(amount: string | number | null | undefined): bigint | null {
  const text =
    typeof amount === 'number'
      ? Number.isFinite(amount)
        ? amount.toFixed(2)
        : ''
      : (amount ?? '').trim();
  const m = /^(-)?(\d+)(?:\.(\d*))?$/.exec(text);
  if (!m) return null;
  const [, neg, int = '0', frac = ''] = m;
  const digits = (frac + '000').slice(0, 3);
  let cents = BigInt(int) * 100n + BigInt(digits.slice(0, 2));
  if (digits.charCodeAt(2) >= 53) cents += 1n; // third digit >= 5
  return neg ? -cents : cents;
}

/** Exact overspend in minor units: 0 when at or under the limit. */
export function overspendMinor(spent: bigint, limit: bigint): bigint {
  return spent > limit ? spent - limit : 0n;
}

/** Format integer minor units: 411465n → "4 114.65" (pl: "4 114,65"). */
export function formatMinor(
  cents: bigint,
  prefs: FormatPrefs,
  opts: { currency?: string } = {},
): string {
  const neg = cents < 0n;
  const abs = neg ? -cents : cents;
  const int = (abs / 100n).toString();
  const frac = (abs % 100n).toString().padStart(2, '0');
  const decimal = isPl(prefs.lang) ? ',' : '.';
  const text = `${neg && abs !== 0n ? MINUS : ''}${group(int, prefs)}${decimal}${frac}`;
  return opts.currency ? `${text} ${opts.currency}` : text;
}

/**
 * Money from an API decimal string. The currency is shown once per section by the caller;
 * pass `currency` only for totals.
 */
export function formatMoney(
  amount: string | number | null | undefined,
  prefs: FormatPrefs,
  opts: { currency?: string } = {},
): string {
  const cents = toMinorUnits(amount);
  return cents === null ? NO_VALUE : formatMinor(cents, prefs, opts);
}

// ---- Product units ----

export type ProductUnit = 'Gram' | 'Milliliter' | 'Piece' | (string & {});

/** API unit enums → display units: g · ml · pcs (pl: szt.). */
export function unitLabel(unit: ProductUnit, count = 2, lang: Lang = 'en'): string {
  switch (unit) {
    case 'Gram':
      return 'g';
    case 'Milliliter':
      return 'ml';
    case 'Piece':
      return isPl(lang) ? 'szt.' : count === 1 ? 'pc' : 'pcs';
    default:
      return String(unit).toLowerCase();
  }
}

/** Amount with a sensible unit: 1200 g → "1.2 kg", 1000 ml → "1 L", 8 Piece → "8 pcs". Fractions are kept. */
export function formatQuantity(
  amount: number | null | undefined,
  unit: ProductUnit,
  prefs: FormatPrefs,
): string {
  if (!finite(amount)) return NO_VALUE;
  if (unit === 'Gram' && Math.abs(amount) >= 1000)
    return `${grouped(amount / 1000, prefs, 0, 2)} kg`;
  if (unit === 'Milliliter' && Math.abs(amount) >= 1000)
    return `${grouped(amount / 1000, prefs, 0, 2)} L`;
  return `${grouped(amount, prefs, 0, unit === 'Piece' ? 2 : 1)} ${unitLabel(unit, amount, prefs.lang)}`;
}

// ---- Dates ----

const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/;

/**
 * A date-only string is a local calendar day (no UTC shift); anything else is an instant and
 * is shown in the viewer's local time. Returns null when it does not parse.
 */
export function toDate(value: Date | string | null | undefined): Date | null {
  if (value === null || value === undefined) return null;
  let d: Date;
  if (value instanceof Date) d = value;
  else if (DATE_ONLY.test(value)) {
    const [y = 0, m = 1, day = 1] = value.split('-').map(Number);
    d = new Date(y, m - 1, day);
  } else d = new Date(value);
  return Number.isNaN(d.getTime()) ? null : d;
}

/** "Sat 3 Oct" (en) / "sob., 3 paź" (pl). */
export function formatDayShort(day: Date | string | null | undefined, lang: Lang): string {
  const d = toDate(day);
  if (!d) return NO_VALUE;
  return sep3(
    new Intl.DateTimeFormat(localeFor(lang), {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
    }).format(d),
  );
}

/** "28 Sep – 4 Oct". */
export function formatDayRange(
  from: Date | string | null | undefined,
  to: Date | string | null | undefined,
  lang: Lang,
): string {
  const f = toDate(from);
  const t = toDate(to);
  if (!f || !t) return NO_VALUE;
  const fmt = new Intl.DateTimeFormat(localeFor(lang), { day: 'numeric', month: 'short' });
  return sep3(`${fmt.format(f)} – ${fmt.format(t)}`);
}

/** Local time from an ISO instant: "21:12". Never shows "UTC". */
export function formatTime(iso: Date | string | null | undefined, lang: Lang): string {
  const d = toDate(iso);
  if (!d) return NO_VALUE;
  return new Intl.DateTimeFormat(localeFor(lang), { hour: '2-digit', minute: '2-digit' }).format(d);
}

// ---- Goals ----

/**
 * Calories, fat, carbs and envelope limits are LIMITS (over = bad); protein and fibre are
 * MINIMUMS (over = met). Within ±tolerance counts as on target.
 */
export type GoalKind = 'limit' | 'min';
export type GoalState = 'over' | 'onTarget' | 'under' | 'met' | 'short' | 'none';

/** Nutrition on-target band (decision record #3). Money is exact and uses no tolerance. */
export const GOAL_TOLERANCE = 0.03;

export function goalStatus(
  value: number | null | undefined,
  target: number | null | undefined,
  kind: GoalKind,
  tolerance = GOAL_TOLERANCE,
): { state: GoalState; diff: number } {
  if (!finite(value) || !finite(target) || target < 0) return { state: 'none', diff: 0 };
  const diff = value - target;
  const rel = target === 0 ? (value === 0 ? 0 : Infinity) : diff / target;
  if (kind === 'limit') {
    if (rel > tolerance) return { state: 'over', diff };
    return { state: rel >= -tolerance ? 'onTarget' : 'under', diff };
  }
  return { state: rel >= -tolerance ? 'met' : 'short', diff };
}
