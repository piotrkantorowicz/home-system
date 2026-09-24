import { clsx, type ClassValue } from 'clsx';
import { extendTailwindMerge } from 'tailwind-merge';

// Named --text-*/--radius-*/--spacing-*/--tracking-* keys added in index.css for the
// redesign scale (#279). Without this, tailwind-merge's default class-group heuristics
// can't tell e.g. `text-13px` (font-size) from a text-color utility and silently drops
// one of them — teach it the exact key list so both survive a `cn()` merge.
const SPACING_SCALE = [
  '3px',
  '15px',
  '18px',
  '22px',
  '30px',
  '34px',
  '38px',
  '42px',
  '52px',
  '60px',
  '100px',
  '132px',
  '180px',
  '220px',
  '280px',
  '420px',
];

const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      size: [{ size: SPACING_SCALE }],
      w: [{ w: SPACING_SCALE }],
      'min-w': [{ 'min-w': SPACING_SCALE }],
      'max-w': [{ 'max-w': SPACING_SCALE }],
      h: [{ h: SPACING_SCALE }],
      'min-h': [{ 'min-h': SPACING_SCALE }],
      'max-h': [{ 'max-h': SPACING_SCALE }],
      gap: [{ gap: SPACING_SCALE }],
      'gap-x': [{ 'gap-x': SPACING_SCALE }],
      'gap-y': [{ 'gap-y': SPACING_SCALE }],
      p: [{ p: SPACING_SCALE }],
      px: [{ px: SPACING_SCALE }],
      py: [{ py: SPACING_SCALE }],
      pt: [{ pt: SPACING_SCALE }],
      pr: [{ pr: SPACING_SCALE }],
      pb: [{ pb: SPACING_SCALE }],
      pl: [{ pl: SPACING_SCALE }],
      m: [{ m: SPACING_SCALE }],
      mx: [{ mx: SPACING_SCALE }],
      my: [{ my: SPACING_SCALE }],
      mt: [{ mt: SPACING_SCALE }],
      mr: [{ mr: SPACING_SCALE }],
      mb: [{ mb: SPACING_SCALE }],
      ml: [{ ml: SPACING_SCALE }],
      'font-size': [
        {
          text: [
            '9-5px',
            '10px',
            '10-5px',
            '11px',
            '11-5px',
            '12px',
            '12-5px',
            '13px',
            '13-5px',
            '14px',
            '15px',
            '17px',
            '19px',
            '20px',
            '22px',
            '26px',
            '34px',
            '0-7rem',
            '0-9rem',
            '0-95rem',
          ],
        },
      ],
      rounded: [
        {
          rounded: [
            '6px',
            '8px',
            '9px',
            '10px',
            '11px',
            '12px',
            '13px',
            '15px',
            '16px',
            '18px',
            '22px',
            '26px',
          ],
        },
      ],
      'rounded-t': [{ 'rounded-t': ['18px'] }],
      'rounded-b': [{ 'rounded-b': ['26px'] }],
      tracking: [{ tracking: ['0-05em'] }],
    },
  },
});

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
