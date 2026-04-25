import { useGoals } from '@modules/diet-planner/api/hooks/useGoals';

import { DayMealList } from './DayMealList';
import { DaySummaryCard } from './DaySummaryCard';

import type { MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

interface Slot {
  id: string;
  name: string;
  defaultTime: string;
  sortOrder: number | string;
}

export interface DayViewProps {
  date: string;
  slots: Slot[];
  meals: MealEntryDto[];
  onAddMeal: (slotId: string) => void;
  onEditMeal: (meal: MealEntryDto) => void;
  onDeleteMeal: (meal: MealEntryDto) => void;
  onCompleteMeal: (meal: MealEntryDto) => void;
  onResetMeal: (meal: MealEntryDto) => void;
  onOverrideMeal: (mealId: string) => void;
}

export function DayView({
  date,
  slots,
  meals,
  onAddMeal,
  onEditMeal,
  onDeleteMeal,
  onCompleteMeal,
  onResetMeal,
  onOverrideMeal,
}: DayViewProps) {
  const { data: goals } = useGoals();
  const dayMeals = meals.filter((m) => m.date === date);

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
      <div className="min-w-0">
        <DayMealList
          slots={slots}
          meals={dayMeals}
          onAddMeal={onAddMeal}
          onEditMeal={onEditMeal}
          onDeleteMeal={onDeleteMeal}
          onCompleteMeal={onCompleteMeal}
          onResetMeal={onResetMeal}
          onOverrideMeal={onOverrideMeal}
        />
      </div>
      <DaySummaryCard meals={dayMeals} goals={goals ?? null} />
    </div>
  );
}
