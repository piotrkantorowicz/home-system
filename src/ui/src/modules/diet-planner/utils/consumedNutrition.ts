import type { DailyNutrition, MealEntryDto } from '../api/hooks/useMeals';

export function isConsumed(meal: Pick<MealEntryDto, 'status'>): boolean {
  return meal.status === 'Done' || meal.status === 'Modified';
}

/** DTO quantities already include portions and actual recipe/product overrides. */
export function consumedNutrition(meals: MealEntryDto[]): DailyNutrition[] {
  const days = new Map<string, DailyNutrition>();
  for (const meal of meals.filter(isConsumed)) {
    const date = meal.date.slice(0, 10);
    const day = days.get(date) ?? { date, calories: 0, protein: 0, carbs: 0, fat: 0, fiber: 0 };
    for (const key of ['calories', 'protein', 'carbs', 'fat', 'fiber'] as const) {
      day[key] += Number(meal[key]);
    }
    days.set(date, day);
  }
  return [...days.values()].sort((a, b) => a.date.localeCompare(b.date));
}
