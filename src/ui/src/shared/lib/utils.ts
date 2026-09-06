import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

const THIN_SPACE = ' ';
const MINUS = '−';

// Mutable so the Preferences "thin-space thousands" toggle can switch every
// formatted figure in the app at once. Defaults to the thin space (the design).
let thousandsSeparator: string = THIN_SPACE;

/** The default (design) thousands separator — U+2009 THIN SPACE. */
export const THIN_SPACE_SEPARATOR = THIN_SPACE;

/** Set the thousands separator used by {@link formatNumber} (Preferences toggle). */
export function setThousandsSeparator(separator: string): void {
  thousandsSeparator = separator;
}

/** Integer with thousands separators: 2150 -> "2 150" (or "2,150" when toggled off). */
export function formatNumber(value: number): string {
  const rounded = Math.round(value);
  const sign = rounded < 0 ? MINUS : '';
  return (
    sign +
    Math.abs(rounded)
      .toString()
      .replace(/\B(?=(\d{3})+(?!\d))/g, thousandsSeparator)
  );
}

/** Signed delta with a real minus sign: -130 → "−130", 200 → "+200", 0 → "0". */
export function formatSigned(value: number): string {
  const rounded = Math.round(value);
  if (rounded === 0) return '0';
  return (rounded > 0 ? '+' : MINUS) + formatNumber(Math.abs(rounded));
}

/**
 * Two-letter initials for an avatar. Falls back to the first two characters of a
 * single-word name, then to "?".
 */
export function getInitials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return '?';
  if (words.length === 1) return (words[0] ?? '').slice(0, 2).toUpperCase();
  return `${words[0]?.[0] ?? ''}${words[words.length - 1]?.[0] ?? ''}`.toUpperCase();
}
