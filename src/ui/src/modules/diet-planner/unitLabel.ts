import type { TFunction } from 'i18next';

const KNOWN_UNITS = ['g', 'ml', 'piece'] as const;

/**
 * Translate a unit value from the API (e.g. "g", "ml", "piece")
 * using the common.unit.* translation keys.
 * Falls back to the raw value for unknown units.
 */
export function unitLabel(unit: string, t: TFunction): string {
  if ((KNOWN_UNITS as readonly string[]).includes(unit)) {
    return t(`common.unit.${unit}`);
  }
  return unit;
}
