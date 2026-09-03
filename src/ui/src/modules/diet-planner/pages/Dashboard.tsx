import { useGoals } from '@modules/diet-planner/api/hooks/useGoals';
import {
  useCreateMeal,
  useMeals,
  useNutritionSummary,
} from '@modules/diet-planner/api/hooks/useMeals';
import { NextUpCard } from '@modules/diet-planner/components/dashboard/NextUpCard';
import { TodayHero } from '@modules/diet-planner/components/dashboard/TodayHero';
import { WaterCard } from '@modules/diet-planner/components/dashboard/WaterCard';
import { WeekReviewCard } from '@modules/diet-planner/components/dashboard/WeekReviewCard';
import { MealForm } from '@modules/diet-planner/components/diet-plans/MealForm';
import { GoalsForm } from '@modules/diet-planner/components/settings';
import {
  Button,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Droplet, Plus } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

function toDateString(date: Date): string {
  return `${String(date.getFullYear())}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(
    date.getDate(),
  ).padStart(2, '0')}`;
}

export default function Dashboard() {
  const { t } = useTranslation();
  const toast = useToast();
  const [goalsSheetOpen, setGoalsSheetOpen] = useState(false);
  const [mealFormOpen, setMealFormOpen] = useState(false);

  const now = new Date();
  const today = toDateString(now);
  const weekAgo = toDateString(new Date(now.getTime() - 6 * 24 * 60 * 60 * 1000));

  const { data: goalsData = null } = useGoals();
  const { data: weekNutrition } = useNutritionSummary({ from: weekAgo, to: today });
  const { data: todayMeals, isLoading: mealsLoading } = useMeals({ from: today, to: today });
  const createMeal = useCreateMeal();

  const week = weekNutrition ?? [];
  const todayNutrition = week.find((d) => d.date.slice(0, 10) === today);

  const dateLabel = now.toLocaleDateString(undefined, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });

  const handleCreateMeal = async (data: {
    date: string;
    mealSlotId: string;
    recipeId: string;
    servings: number;
    notes: string;
  }) => {
    try {
      await createMeal.mutateAsync({ ...data, mealTime: null, sequenceOrder: null });
      toast.success(t('meal_form.add_success'));
      setMealFormOpen(false);
    } catch {
      toast.error(t('meal_form.add_error'));
    }
  };

  return (
    <div className="animate-fade-in flex flex-col gap-6 px-4 py-6 md:px-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[30px] font-bold">{t('dashboard.today_title')}</h1>
          <p className="text-muted-foreground mt-1 text-sm">{dateLabel}</p>
        </div>
        <div className="flex gap-2.5">
          <Button size="xl" variant="outline" asChild>
            <Link to="/diet-planner/hydration">
              <Droplet className="size-4" />
              {t('dashboard.log_water')}
            </Link>
          </Button>
          <Button
            size="xl"
            onClick={() => {
              setMealFormOpen(true);
            }}
          >
            <Plus className="size-4" />
            {t('dashboard.log_meal')}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 items-start gap-[18px] xl:grid-cols-2">
        <TodayHero
          goals={goalsData}
          nutrition={todayNutrition}
          onSetGoals={() => {
            setGoalsSheetOpen(true);
          }}
        />
        <NextUpCard
          meals={todayMeals ?? []}
          loading={mealsLoading}
          onAddMeal={() => {
            setMealFormOpen(true);
          }}
        />
        <WaterCard />
        <WeekReviewCard week={week} target={goalsData?.dailyCalorieTarget ?? null} />
      </div>

      <Sheet open={goalsSheetOpen} onOpenChange={setGoalsSheetOpen}>
        <SheetContent side="right">
          <SheetHeader>
            <SheetTitle>{t('sheets.goals.title')}</SheetTitle>
            <SheetDescription>{t('sheets.goals.description')}</SheetDescription>
          </SheetHeader>
          <div className="mt-6 overflow-y-auto">
            <GoalsForm
              onSuccess={() => {
                setGoalsSheetOpen(false);
              }}
            />
          </div>
        </SheetContent>
      </Sheet>

      <MealForm
        open={mealFormOpen}
        mode="create"
        initialDate={today}
        onClose={() => {
          setMealFormOpen(false);
        }}
        onSubmit={(data) => {
          void handleCreateMeal(data);
        }}
        isSubmitting={createMeal.isPending}
      />
    </div>
  );
}
