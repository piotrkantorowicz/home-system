/**
 * One way to write every number, unit and date in HomeSystem.
 * See SPEC.md § Foundations › Formatting. Pure functions — no React, no i18n
 * instance; pass the language and the user's prefs in.
 *
 * Prefs map to the existing `home-system-prefs` store:
 *   thinSpaceThousands: true  → "2 150"   false → "2,150" (en) / "2 150" (pl uses space anyway)
 */

export type Lang = 'en' | 'pl' | (string & {});
export interface NumberPrefs {
  lang: Lang;
  thinSpaceThousands: boolean;
}

const THIN_NBSP = ' '; // narrow no-break space: never wraps inside a number

function localeFor(lang: Lang): string {
  return lang === 'pl' ? 'pl-PL' : 'en-GB';
}

/**
 * Grouping is done by hand: Intl's pl-PL leaves 4-digit numbers ungrouped
 * ("4114,65"), which would make columns inconsistent.
 */
function grouped(value: number, prefs: NumberPrefs, fraction: { min: number; max: number }): string {
  const parts = new Intl.NumberFormat(localeFor(prefs.lang), {
    minimumFractionDigits: fraction.min,
    maximumFractionDigits: fraction.max,
    useGrouping: false,
  }).formatToParts(Math.abs(value));
  const intPart = parts.filter((p) => p.type === 'integer').map((p) => p.value).join('');
  const decimal = parts.find((p) => p.type === 'decimal')?.value ?? '';
  const frac = parts.filter((p) => p.type === 'fraction').map((p) => p.value).join('');
  const sep = prefs.lang === 'pl' || prefs.thinSpaceThousands ? THIN_NBSP : ',';
  const withGroups = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, sep);
  const sign = value < 0 && Math.abs(value) > 0 ? '−' : ''; // real minus sign
  return sign + withGroups + (frac ? decimal + frac : '');
}

/** Energy: always an integer. 2100 → "2 100". */
export function formatKcal(kcal: number, prefs: NumberPrefs): string {
  return grouped(Math.round(kcal), prefs, { min: 0, max: 0 });
}

/** Grams of a macro: one decimal only under 10, otherwise integer. 46.0 → "46", 4.54 → "4.5", 0 → "0". */
export function formatGrams(g: number, prefs: NumberPrefs): string {
  if (g === 0) return '0';
  if (Math.abs(g) < 10) {
    const rounded = Math.round(g * 10) / 10;
    return grouped(rounded, prefs, { min: Number.isInteger(rounded) ? 0 : 1, max: 1 });
  }
  return grouped(Math.round(g), prefs, { min: 0, max: 0 });
}

/** Water: litres with one decimal. 1330 → "1.3". */
export function formatLitres(ml: number, prefs: NumberPrefs): string {
  return grouped(Math.round(ml / 100) / 10, prefs, { min: 1, max: 1 });
}

/** "1.3 of 2.5 L" — the ONE way water progress is written. */
export function formatWaterProgress(ml: number, goalMl: number, prefs: NumberPrefs, ofWord = 'of'): string {
  return `${formatLitres(ml, prefs)} ${ofWord} ${formatLitres(goalMl, prefs)} L`;
}

/**
 * Money. API amounts are decimal strings ("184.36"); never round-trip through
 * floats for arithmetic — use cents. This only formats.
 * Currency is shown once per section by the caller; pass withCurrency only for totals.
 */
export function formatMoney(
  amount: string | number,
  prefs: NumberPrefs,
  opts: { currency?: string; withCurrency?: boolean } = {},
): string {
  const value = typeof amount === 'string' ? Number(amount) : amount;
  const text = grouped(value, prefs, { min: 2, max: 2 });
  return opts.withCurrency && opts.currency ? `${text} ${opts.currency}` : text;
}

/** API unit enums → display units. */
export type ProductUnit = 'Gram' | 'Milliliter' | 'Piece' | (string & {});
export function unitLabel(unit: ProductUnit, count = 2): string {
  switch (unit) {
    case 'Gram': return 'g';
    case 'Milliliter': return 'ml';
    case 'Piece': return count === 1 ? 'pc' : 'pcs';
    default: return String(unit).toLowerCase();
  }
}

/** Amount with a sensible unit: 1200 g → "1.2 kg", 1000 ml → "1 L", 8 Piece → "8 pcs". */
export function formatQuantity(amount: number, unit: ProductUnit, prefs: NumberPrefs): string {
  if (unit === 'Gram' && amount >= 1000) return `${grouped(amount / 1000, prefs, { min: 0, max: 2 })} kg`;
  if (unit === 'Milliliter' && amount >= 1000) return `${grouped(amount / 1000, prefs, { min: 0, max: 2 })} L`;
  return `${grouped(amount, prefs, { min: 0, max: unit === 'Piece' ? 0 : 1 })} ${unitLabel(unit, amount)}`;
}

/** Parse "YYYY-MM-DD" as a local calendar date (no UTC shift). */
export function parseDay(isoDay: string): Date {
  const [y, m, d] = isoDay.slice(0, 10).split('-').map(Number);
  return new Date(y, (m ?? 1) - 1, d ?? 1);
}

/** "Sat 3 Oct" (en) / "sob. 3 paź" (pl). Use in tables, lists and headers. */
export function formatDayShort(day: Date | string, lang: Lang): string {
  const d = typeof day === 'string' ? parseDay(day) : day;
  return new Intl.DateTimeFormat(localeFor(lang), { weekday: 'short', day: 'numeric', month: 'short' }).format(d);
}

/** "28 Sep – 4 Oct". */
export function formatDayRange(from: Date | string, to: Date | string, lang: Lang): string {
  const f = typeof from === 'string' ? parseDay(from) : from;
  const t = typeof to === 'string' ? parseDay(to) : to;
  const fmt = new Intl.DateTimeFormat(localeFor(lang), { day: 'numeric', month: 'short' });
  return `${fmt.format(f)} – ${fmt.format(t)}`;
}

/** Local time from an ISO timestamp: "21:12". Never show "UTC" to users. */
export function formatTime(iso: string, lang: Lang): string {
  return new Intl.DateTimeFormat(localeFor(lang), { hour: '2-digit', minute: '2-digit' }).format(new Date(iso));
}

/**
 * Goal direction. Calories / fat / carbs / envelope limits are LIMITS;
 * protein / fiber are MINIMUMS. Within ±tolerance counts as on target.
 */
export type GoalKind = 'limit' | 'min';
export type GoalState = 'over' | 'onTarget' | 'under' | 'met' | 'short';
export function goalStatus(value: number, target: number, kind: GoalKind, tolerance = 0.03): { state: GoalState; diff: number } {
  const diff = value - target;
  const rel = target === 0 ? (value === 0 ? 0 : Infinity) : diff / target;
  if (kind === 'limit') {
    if (rel > tolerance) return { state: 'over', diff };
    if (rel >= -tolerance) return { state: 'onTarget', diff };
    return { state: 'under', diff };
  }
  if (rel >= -tolerance) return { state: 'met', diff };
  return { state: 'short', diff };
}
