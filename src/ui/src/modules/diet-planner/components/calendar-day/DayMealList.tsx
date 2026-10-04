import {
  Badge,
  Card,
  CardContent,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import { Check, Ellipsis, Pencil, Plus, RotateCcw, Sparkles, Trash2 } from 'lucide-react';
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
  /** Add, edit, delete controls (default true). */
  canPlan?: boolean;
  /** Complete, override, reset controls (default true). */
  canLog?: boolean;
  /** Badge each meal with its person — set when viewing someone else's plan. */
  showPerson?: boolean;
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
  canPlan = true,
  canLog = true,
  showPerson = false,
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
              {canPlan && (
                <button
                  type="button"
                  onClick={() => {
                    onAddMeal(slot.id);
                  }}
                  className="text-muted-foreground hover:text-primary focus-visible:ring-primary -my-2 grid size-11 place-items-center rounded-md text-xs transition-colors focus-visible:ring-2 focus-visible:outline-none"
                  aria-label={t('meal_form.add_title')}
                  title={t('meal_form.add_title')}
                >
                  <Plus className="size-5" />
                </button>
              )}
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
                  access={{ canPlan, canLog, showPerson }}
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
  access: { canPlan: boolean; canLog: boolean; showPerson: boolean };
  onEdit: () => void;
  onDelete: () => void;
  onComplete: () => void;
  onReset: () => void;
  onOverride: () => void;
}

function MealRow({
  meal,
  access: { canPlan, canLog, showPerson },
  onEdit,
  onDelete,
  onComplete,
  onReset,
  onOverride,
}: MealRowProps) {
  const { t } = useTranslation();
  const status = (meal.status as MealStatus | undefined) ?? 'Planned';
  const isModified = status === 'Modified' && meal.actualRecipe;
  const consumed = status === 'Done' || status === 'Modified';
  const name = meal.actualRecipe?.name ?? meal.recipeName;

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
                className="text-muted-foreground/70 block truncate text-xs"
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
          {showPerson && meal.personName !== null && (
            <Badge variant="secondary" className="mt-1">
              {meal.personName}
            </Badge>
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

        <div className="flex shrink-0 items-center gap-0.5">
          <span className="numeral mr-1 text-base font-semibold">
            {Math.round(num(meal.calories))}
            <span className="text-muted-foreground ml-1 text-xs font-normal">kcal</span>
          </span>
          {canLog && (
            <button
              type="button"
              aria-pressed={consumed}
              aria-label={t(consumed ? 'dashboard.meal_unmark' : 'dashboard.meal_mark', { name })}
              onClick={consumed ? onReset : onComplete}
              className="focus-visible:ring-ring grid size-11 shrink-0 place-items-center rounded-full focus-visible:ring-2 focus-visible:outline-none"
            >
              <span
                className={cn(
                  'grid size-7 place-items-center rounded-full border-2',
                  consumed ? 'bg-primary border-primary text-primary-foreground' : 'border-input',
                )}
              >
                {consumed && <Check className="size-4" strokeWidth={3} />}
              </span>
            </button>
          )}
          {(canLog || canPlan) && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button
                  type="button"
                  aria-label={t('calendar.meal_actions.menu_for', { name })}
                  className="text-muted-foreground hover:text-foreground focus-visible:ring-ring grid size-11 shrink-0 place-items-center rounded-full focus-visible:ring-2 focus-visible:outline-none"
                >
                  <Ellipsis className="size-5" />
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="min-w-[180px]">
                {canLog && (
                  <DropdownMenuItem onSelect={onOverride}>
                    <Sparkles />
                    {t('calendar.meal_actions.override')}
                  </DropdownMenuItem>
                )}
                {canLog && status !== 'Planned' && (
                  <DropdownMenuItem onSelect={onReset}>
                    <RotateCcw />
                    {t('calendar.meal_actions.reset')}
                  </DropdownMenuItem>
                )}
                {canPlan && (
                  <>
                    {canLog && <DropdownMenuSeparator />}
                    <DropdownMenuItem onSelect={onEdit}>
                      <Pencil />
                      {t('common.edit')}
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      onSelect={onDelete}
                      className="text-destructive focus:text-destructive"
                    >
                      <Trash2 />
                      {t('common.delete')}
                    </DropdownMenuItem>
                  </>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          )}
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
