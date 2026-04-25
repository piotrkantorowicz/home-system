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
  slots: Slot[];
  // Caller is responsible for passing meals already scoped to the displayed day.
  // We deliberately do NOT re-filter here: the parent fetches a single-day query,
  // and TanStack Query's keepPreviousData would otherwise leave stale entries from
  // the previous day flagged with the wrong date, causing the summary to flash to
  // zero on every day change.
  meals: MealEntryDto[];
  onAddMeal: (slotId: string) => void;
  onEditMeal: (meal: MealEntryDto) => void;
  onDeleteMeal: (meal: MealEntryDto) => void;
  onCompleteMeal: (meal: MealEntryDto) => void;
  onResetMeal: (meal: MealEntryDto) => void;
  onOverrideMeal: (mealId: string) => void;
}

export function DayView({
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

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
      <div className="min-w-0">
        <DayMealList
          slots={slots}
          meals={meals}
          onAddMeal={onAddMeal}
          onEditMeal={onEditMeal}
          onDeleteMeal={onDeleteMeal}
          onCompleteMeal={onCompleteMeal}
          onResetMeal={onResetMeal}
          onOverrideMeal={onOverrideMeal}
        />
      </div>
      <DaySummaryCard meals={meals} goals={goals ?? null} />
    </div>
  );
}
