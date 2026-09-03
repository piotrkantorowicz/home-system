import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

const THIN_SPACE = ' ';
const MINUS = '−';

/** Integer with thin-space thousands separators: 2150 → "2 150". */
export function formatNumber(value: number): string {
  const rounded = Math.round(value);
  const sign = rounded < 0 ? MINUS : '';
  return (
    sign +
    Math.abs(rounded)
      .toString()
      .replace(/\B(?=(\d{3})+(?!\d))/g, THIN_SPACE)
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
