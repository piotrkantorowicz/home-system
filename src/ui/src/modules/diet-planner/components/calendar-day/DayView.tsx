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
  // Caller passes resolved meals for this date; placeholder data is hidden while loading.
  meals: MealEntryDto[];
  onAddMeal: (slotId: string) => void;
  onEditMeal: (meal: MealEntryDto) => void;
  onDeleteMeal: (meal: MealEntryDto) => void;
  onCompleteMeal: (meal: MealEntryDto) => void;
  onResetMeal: (meal: MealEntryDto) => void;
  onOverrideMeal: (mealId: string) => void;
  canPlan?: boolean;
  canLog?: boolean;
  /** Viewing someone else's plan: badge meals, hide the caller-only summary card. */
  otherPerson?: boolean;
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
  canPlan = true,
  canLog = true,
  otherPerson = false,
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
          canPlan={canPlan}
          canLog={canLog}
          showPerson={otherPerson}
        />
      </div>
      {!otherPerson && <DaySummaryCard date={date} meals={meals} goals={goals ?? null} />}
    </div>
  );
}
