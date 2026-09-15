import { Card, CardContent } from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import { Check, Pencil, Plus, RotateCcw, Sparkles, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { MealStatusBadge, type MealStatus } from '../diet-plans/MealStatusBadge';

import type { MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

interface Slot {
  id: string;
  name: string;
  defaultTime: string;
  sortOrder: number | string;
}

export interface DayMealListProps {
  slots: Slot[];
  meals: MealEntryDto[];
  onAddMeal: (slotId: string) => void;
  onEditMeal: (meal: MealEntryDto) => void;
  onDeleteMeal: (meal: MealEntryDto) => void;
  onCompleteMeal: (meal: MealEntryDto) => void;
  onResetMeal: (meal: MealEntryDto) => void;
  onOverrideMeal: (mealId: string) => void;
}

function num(v: number | string | null | undefined): number {
  if (v === null || v === undefined) return 0;
  return typeof v === 'number' ? v : Number(v);
}

export function DayMealList({
  slots,
  meals,
  onAddMeal,
  onEditMeal,
  onDeleteMeal,
  onCompleteMeal,
  onResetMeal,
  onOverrideMeal,
}: DayMealListProps) {
  const { t } = useTranslation();

  const mealsBySlot = meals.reduce<Record<string, MealEntryDto[]>>((acc, meal) => {
    const slotMeals = acc[meal.mealSlotId] ?? [];
    slotMeals.push(meal);
    acc[meal.mealSlotId] = slotMeals;
    return acc;
  }, {});

  if (slots.length === 0) {
    return (
      <div className="text-muted-foreground/60 rounded-lg border border-dashed p-8 text-center text-sm">
        {t('calendar.no_schedule')}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {slots.map((slot) => {
        const slotMeals = mealsBySlot[slot.id] ?? [];
        return (
          <section key={slot.id} className="space-y-2">
            <div className="flex items-baseline justify-between">
              <h3 className="text-sm font-semibold">
                {slot.name}{' '}
                <span className="text-muted-foreground ml-1 text-xs font-normal">
                  {slot.defaultTime.slice(0, 5)}
                </span>
              </h3>
              <button
                type="button"
                onClick={() => {
                  onAddMeal(slot.id);
                }}
                className="text-muted-foreground hover:text-primary focus-visible:ring-primary rounded-md p-1 text-xs transition-colors focus-visible:ring-2 focus-visible:outline-none"
                aria-label={t('meal_form.add_title')}
                title={t('meal_form.add_title')}
              >
                <Plus className="h-4 w-4" />
              </button>
            </div>

            {slotMeals.length === 0 ? (
              <div className="text-muted-foreground/60 rounded-lg border border-dashed p-3 text-center text-xs">
                {t('diet_plan_detail.no_meal')}
              </div>
            ) : (
              slotMeals.map((meal) => (
                <MealRow
                  key={meal.id}
                  meal={meal}
                  onEdit={() => {
                    onEditMeal(meal);
                  }}
                  onDelete={() => {
                    onDeleteMeal(meal);
                  }}
                  onComplete={() => {
                    onCompleteMeal(meal);
                  }}
                  onReset={() => {
                    onResetMeal(meal);
                  }}
                  onOverride={() => {
                    onOverrideMeal(meal.id);
                  }}
                />
              ))
            )}
          </section>
        );
      })}
    </div>
  );
}

interface MealRowProps {
  meal: MealEntryDto;
  onEdit: () => void;
  onDelete: () => void;
  onComplete: () => void;
  onReset: () => void;
  onOverride: () => void;
}

function MealRow({ meal, onEdit, onDelete, onComplete, onReset, onOverride }: MealRowProps) {
  const { t } = useTranslation();
  const status = (meal.status as MealStatus | undefined) ?? 'Planned';
  const isModified = status === 'Modified' && meal.actualRecipe;

  return (
    <Card className="hover:border-primary/40 transition-colors">
      <CardContent className="flex items-start gap-3 p-3">
        <MealStatusBadge status={status} className="mt-1 shrink-0" />

        <div className="min-w-0 flex-1">
          {isModified && meal.actualRecipe ? (
            <>
              <Link
                to={`/diet-planner/recipes/${meal.actualRecipe.id}`}
                title={meal.actualRecipe.name}
                className="block font-medium hover:underline"
              >
                {meal.actualRecipe.name}
              </Link>
              <span
                title={meal.recipeName}
                className="text-muted-foreground/70 block truncate text-xs line-through"
              >
                {meal.recipeName}
              </span>
            </>
          ) : (
            <Link
              to={`/diet-planner/recipes/${meal.recipeId}`}
              title={meal.recipeName}
              className="block font-medium hover:underline"
            >
              {meal.recipeName}
            </Link>
          )}
          <p className="text-muted-foreground mt-0.5 text-xs">
            {t('recipes.servings', { count: num(meal.servings) || 1 })}
            {meal.notes && ` · ${meal.notes}`}
          </p>
          <div className="text-muted-foreground mt-2 flex flex-wrap gap-x-3 gap-y-1 text-xs">
            <Macro label={t('day_view.macro.protein')} value={num(meal.protein)} />
            <Macro label={t('day_view.macro.carbs')} value={num(meal.carbs)} />
            <Macro label={t('day_view.macro.fat')} value={num(meal.fat)} />
            {num(meal.fiber) > 0 && (
              <Macro label={t('day_view.macro.fiber')} value={num(meal.fiber)} />
            )}
          </div>
        </div>

        <div className="flex shrink-0 flex-col items-end gap-2">
          <span className={cn('text-base font-semibold tabular-nums')}>
            {Math.round(num(meal.calories))}
            <span className="text-muted-foreground ml-1 text-xs font-normal">kcal</span>
          </span>
          <div className="flex items-center gap-1">
            {status !== 'Done' && status !== 'Modified' && (
              <ActionButton
                onClick={onComplete}
                title={t('calendar.meal_actions.mark_done')}
                hoverColor="hover:text-emerald-600"
              >
                <Check className="h-3.5 w-3.5" />
              </ActionButton>
            )}
            <ActionButton
              onClick={onOverride}
              title={t('calendar.meal_actions.override')}
              hoverColor="hover:text-amber-600"
            >
              <Sparkles className="h-3.5 w-3.5" />
            </ActionButton>
            {status !== 'Planned' && (
              <ActionButton
                onClick={onReset}
                title={t('calendar.meal_actions.reset')}
                hoverColor="hover:text-foreground"
              >
                <RotateCcw className="h-3.5 w-3.5" />
              </ActionButton>
            )}
            <ActionButton
              onClick={onEdit}
              title={t('common.edit')}
              hoverColor="hover:text-foreground"
            >
              <Pencil className="h-3.5 w-3.5" />
            </ActionButton>
            <ActionButton
              onClick={onDelete}
              title={t('common.delete')}
              hoverColor="hover:text-destructive"
            >
              <Trash2 className="h-3.5 w-3.5" />
            </ActionButton>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

interface MacroProps {
  label: string;
  value: number;
}

function Macro({ label, value }: MacroProps) {
  return (
    <span>
      <span className="text-muted-foreground/70">{label}</span>{' '}
      <span className="text-foreground font-medium tabular-nums">{value.toFixed(1)}g</span>
    </span>
  );
}

interface ActionButtonProps {
  onClick: () => void;
  title: string;
  hoverColor: string;
  children: React.ReactNode;
}

function ActionButton({ onClick, title, hoverColor, children }: ActionButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={title}
      aria-label={title}
      className={cn(
        'text-muted-foreground focus-visible:ring-primary rounded p-1 transition-colors focus-visible:ring-2 focus-visible:outline-none',
        hoverColor,
      )}
    >
      {children}
    </button>
  );
}
