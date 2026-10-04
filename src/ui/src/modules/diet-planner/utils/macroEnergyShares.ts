type NumLike = number | string | null | undefined;
type MacroGrams = { protein: NumLike; carbs: NumLike; fat: NumLike } | null | undefined;

const toNum = (v: NumLike): number | null => {
  if (v === null || v === undefined || v === '') return null;
  const x = Number(v);
  return Number.isFinite(x) ? x : null;
};

/** Share of energy from each macro (protein/carbs 4 kcal/g, fat 9 kcal/g); null when it cannot be computed. */
export function macroEnergyShares(
  per: MacroGrams,
): { protein: number; carbs: number; fat: number } | null {
  const protein = toNum(per?.protein);
  const carbs = toNum(per?.carbs);
  const fat = toNum(per?.fat);
  if (protein === null || carbs === null || fat === null) return null;
  const p = protein * 4;
  const c = carbs * 4;
  const f = fat * 9;
  const total = p + c + f;
  if (total <= 0) return null;
  return { protein: (p / total) * 100, carbs: (c / total) * 100, fat: (f / total) * 100 };
}
