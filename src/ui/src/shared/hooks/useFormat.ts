import { usePreferences } from '@shared/hooks/usePreferences';
import * as format from '@shared/lib/format';
import { useTranslation } from 'react-i18next';

/** The format helpers bound to the active language and the user's unit/grouping preferences. */
export function useFormat() {
  const { i18n } = useTranslation();
  const { prefs } = usePreferences();
  const lang = i18n.language;
  const fp: format.FormatPrefs = {
    lang,
    thinSpaceThousands: prefs.thinSpaceThousands,
    energyUnit: prefs.energyUnit,
    weightUnit: prefs.weightUnit,
    volumeUnit: prefs.volumeUnit,
  };

  return {
    energy: (kcal: number | null | undefined) => format.formatEnergy(kcal, fp),
    grams: (g: number | null | undefined) => format.formatGrams(g, fp),
    weight: (kg: number | null | undefined) => format.formatWeight(kg, fp),
    volume: (ml: number | null | undefined) => format.formatVolume(ml, fp),
    waterProgress: (
      ml: number | null | undefined,
      goalMl: number | null | undefined,
      ofWord?: string,
    ) => format.formatWaterProgress(ml, goalMl, fp, ofWord),
    money: (amount: string | number | null | undefined, opts?: { currency?: string }) =>
      format.formatMoney(amount, fp, opts),
    quantity: (amount: number | null | undefined, unit: format.ProductUnit) =>
      format.formatQuantity(amount, unit, fp),
    unit: (unit: format.ProductUnit, count?: number) => format.unitLabel(unit, count, lang),
    dayShort: (day: Date | string | null | undefined) => format.formatDayShort(day, lang),
    dayRange: (from: Date | string | null | undefined, to: Date | string | null | undefined) =>
      format.formatDayRange(from, to, lang),
    time: (iso: Date | string | null | undefined) => format.formatTime(iso, lang),
  } as const;
}
