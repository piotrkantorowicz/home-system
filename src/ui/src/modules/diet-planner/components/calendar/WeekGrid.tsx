import {
  Card,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Skeleton,
} from '@shared/components/ui';
import { cn, formatNumber, formatSigned } from '@shared/lib/utils';
import { Check, CheckCheck, Pencil, Plus, RotateCcw, Sparkles, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { DailyNutrition, MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

type Macro = 'protein' | 'carbs' | 'fat' | 'fiber';

interface Slot {
  id: string;
  name: string;
  sortOrder: number | string;
}

interface WeekGridProps {
  weekDays: Date[];
  slots: Slot[];
  meals: MealEntryDto[];
  nutritionByDate: Map<string, DailyNutrition>;
  calorieTarget: number | null;
  loading: boolean;
  bulkPending: boolean;
  onAddMeal: (date: string, slotId: string) => void;
  onEditMeal: (meal: MealEntryDto) => void;
  onCompleteMeal: (meal: MealEntryDto) => void;
  onResetMeal: (meal: MealEntryDto) => void;
  onOverrideMeal: (mealId: string) => void;
  onDeleteMeal: (meal: MealEntryDto) => void;
  onBulkComplete: (date: string) => void;
}

const num = (v: number | string | null | undefined): number =>
  typeof v === 'number' ? v : Number(v ?? 0);

function toDateStr(date: Date): string {
  return `${String(date.getFullYear())}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(
    date.getDate(),
  ).padStart(2, '0')}`;
}

function isDone(meal: MealEntryDto): boolean {
  return meal.status === 'Done' || meal.status === 'Modified';
}

function dominantMacro(meals: MealEntryDto[]): Macro {
  const totals = meals.reduce(
    (acc, m) => ({
      protein: acc.protein + num(m.protein),
      carbs: acc.carbs + num(m.carbs),
      fat: acc.fat + num(m.fat),
      fiber: acc.fiber + num(m.fiber),
    }),
    { protein: 0, carbs: 0, fat: 0, fiber: 0 },
  );
  if (totals.fiber >= 8 && totals.fiber >= totals.protein) return 'fiber';
  const macro = (['protein', 'carbs', 'fat'] as const).reduce((best, key) =>
    totals[key] > totals[best] ? key : best,
  );
  return macro;
}

const macroTint: Record<Macro, string> = {
  protein: 'color-mix(in oklab, var(--color-protein) 16%, transparent)',
  carbs: 'color-mix(in oklab, var(--color-carbs) 16%, transparent)',
  fat: 'color-mix(in oklab, var(--color-fat) 16%, transparent)',
  fiber: 'color-mix(in oklab, var(--color-fiber) 16%, transparent)',
};

export function WeekGrid({
  weekDays,
  slots,
  meals,
  nutritionByDate,
  calorieTarget,
  loading,
  bulkPending,
  onAddMeal,
  onEditMeal,
  onCompleteMeal,
  onResetMeal,
  onOverrideMeal,
  onDeleteMeal,
  onBulkComplete,
}: WeekGridProps) {
  const { t, i18n } = useTranslation();
  const todayStr = toDateStr(new Date());

  const byDaySlot = new Map<string, MealEntryDto[]>();
  for (const meal of meals) {
    const key = `${meal.date}|${meal.mealSlotId}`;
    const list = byDaySlot.get(key) ?? [];
    list.push(meal);
    byDaySlot.set(key, list);
  }

  const columns = `92px repeat(${String(weekDays.length)}, minmax(0, 1fr))`;

  if (loading) {
    return (
      <Card className="p-4">
        <Skeleton className="h-420px w-full" />
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <Card className="overflow-x-auto p-0">
        <div
          role="grid"
          aria-label={t('calendar.view.week')}
          className="grid min-w-[840px]"
          style={{ gridTemplateColumns: columns }}
        >
          {/* Header row */}
          <div className="border-border border-b" />
          {weekDays.map((date) => {
            const dateStr = toDateStr(date);
            const isToday = dateStr === todayStr;
            return (
              <div
                key={dateStr}
                role="columnheader"
                aria-label={date.toLocaleDateString(i18n.language, { weekday: 'short' })}
                className={cn(
                  'border-border border-b border-l px-2.5 py-3.5 text-center',
                  isToday && 'bg-accent',
                )}
              >
                <div className={cn('text-12px font-bold', isToday && 'text-accent-foreground')}>
                  {date.toLocaleDateString(i18n.language, { weekday: 'short' })}
                </div>
                <div
                  className={cn(
                    'text-11px',
                    isToday ? 'text-accent-foreground/80' : 'text-muted-foreground',
                  )}
                >
                  {date.toLocaleDateString(i18n.language, { day: 'numeric', month: 'short' })}
                  {isToday ? ` · ${t('calendar.week_grid.today')}` : ''}
                </div>
              </div>
            );
          })}

          {/* Slot rows */}
          {slots.map((slot) => (
            <FragmentRow
              key={slot.id}
              slot={slot}
              weekDays={weekDays}
              todayStr={todayStr}
              byDaySlot={byDaySlot}
              onAddMeal={onAddMeal}
              onEditMeal={onEditMeal}
              onCompleteMeal={onCompleteMeal}
              onResetMeal={onResetMeal}
              onOverrideMeal={onOverrideMeal}
              onDeleteMeal={onDeleteMeal}
            />
          ))}

          {/* Day total row */}
          <div className="text-muted-foreground text-12px px-3 py-3 font-bold">
            {t('calendar.week_grid.day_total')}
          </div>
          {weekDays.map((date) => {
            const dateStr = toDateStr(date);
            const isToday = dateStr === todayStr;
            const kcal = Math.round(nutritionByDate.get(dateStr)?.calories ?? 0);
            const delta = calorieTarget !== null && kcal > 0 ? kcal - calorieTarget : null;
            const hasPlanned = meals.some((m) => m.date === dateStr && m.status === 'Planned');
            return (
              <div
                key={dateStr}
                className={cn(
                  'border-border relative border-l px-2.5 py-3 text-center',
                  isToday && 'bg-accent',
                )}
              >
                <div className="numeral text-12-5px font-bold">{formatNumber(kcal)}</div>
                <div
                  className={cn(
                    'text-10-5px font-semibold',
                    delta === null
                      ? 'text-muted-foreground'
                      : delta > 0
                        ? 'text-destructive'
                        : 'text-success',
                  )}
                >
                  {delta === null ? t('calendar.week_grid.gap') : formatSigned(delta)}
                </div>
                {hasPlanned ? (
                  <button
                    type="button"
                    onClick={() => {
                      onBulkComplete(dateStr);
                    }}
                    disabled={bulkPending}
                    title={t('calendar.bulk_complete.button')}
                    aria-label={t('calendar.bulk_complete.button')}
                    className="text-muted-foreground hover:text-success absolute top-1.5 right-1.5 rounded p-0.5 transition-colors"
                  >
                    <CheckCheck className="size-3.5" />
                  </button>
                ) : null}
              </div>
            );
          })}
        </div>
      </Card>

      {/* Legend */}
      <div className="border-border bg-card rounded-18px text-12px flex flex-wrap items-center gap-4 border px-4 py-3.5">
        <span className="font-semibold">{t('calendar.week_grid.legend')}</span>
        <LegendSwatch macro="carbs" label={t('calendar.week_grid.carb_led')} />
        <LegendSwatch macro="protein" label={t('calendar.week_grid.protein_led')} />
        <LegendSwatch macro="fiber" label={t('calendar.week_grid.veg_led')} />
      </div>
    </div>
  );
}

interface FragmentRowProps {
  slot: Slot;
  weekDays: Date[];
  todayStr: string;
  byDaySlot: Map<string, MealEntryDto[]>;
  onAddMeal: (date: string, slotId: string) => void;
  onEditMeal: (meal: MealEntryDto) => void;
  onCompleteMeal: (meal: MealEntryDto) => void;
  onResetMeal: (meal: MealEntryDto) => void;
  onOverrideMeal: (mealId: string) => void;
  onDeleteMeal: (meal: MealEntryDto) => void;
}

function FragmentRow({
  slot,
  weekDays,
  todayStr,
  byDaySlot,
  onAddMeal,
  onEditMeal,
  onCompleteMeal,
  onResetMeal,
  onOverrideMeal,
  onDeleteMeal,
}: FragmentRowProps) {
  const { t, i18n } = useTranslation();
  return (
    <>
      <div
        role="rowheader"
        className="border-border text-muted-foreground text-12px border-t px-3 py-3 font-bold"
      >
        {slot.name}
      </div>
      {weekDays.map((date) => {
        const dateStr = toDateStr(date);
        const isToday = dateStr === todayStr;
        const cellMeals = byDaySlot.get(`${dateStr}|${slot.id}`) ?? [];
        return (
          <div
            key={dateStr}
            role="gridcell"
            aria-label={`${date.toLocaleDateString(i18n.language, { weekday: 'short' })} ${slot.name}`}
            className={cn('border-border border-t border-l p-2', isToday && 'bg-accent/40')}
          >
            {cellMeals.length === 0 ? (
              <button
                type="button"
                onClick={() => {
                  onAddMeal(dateStr, slot.id);
                }}
                className="border-border-strong text-muted-foreground hover:text-foreground hover:border-foreground/40 min-h-52px rounded-12px text-17px grid w-full place-items-center border border-dashed transition-colors"
                title={t('meal_form.add_title')}
                aria-label={t('meal_form.add_title')}
              >
                <Plus className="size-4" />
              </button>
            ) : (
              <div className="flex flex-col gap-1.5">
                {cellMeals.map((meal) => (
                  <MealChip
                    key={meal.id}
                    meal={meal}
                    onEditMeal={onEditMeal}
                    onCompleteMeal={onCompleteMeal}
                    onResetMeal={onResetMeal}
                    onOverrideMeal={onOverrideMeal}
                    onDeleteMeal={onDeleteMeal}
                  />
                ))}
              </div>
            )}
          </div>
        );
      })}
    </>
  );
}

interface MealChipProps {
  meal: MealEntryDto;
  onEditMeal: (meal: MealEntryDto) => void;
  onCompleteMeal: (meal: MealEntryDto) => void;
  onResetMeal: (meal: MealEntryDto) => void;
  onOverrideMeal: (mealId: string) => void;
  onDeleteMeal: (meal: MealEntryDto) => void;
}

function MealChip({
  meal,
  onEditMeal,
  onCompleteMeal,
  onResetMeal,
  onOverrideMeal,
  onDeleteMeal,
}: MealChipProps) {
  const { t } = useTranslation();
  const done = isDone(meal);
  const name =
    meal.status === 'Modified' && meal.actualRecipe ? meal.actualRecipe.name : meal.recipeName;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          className={cn(
            'rounded-12px text-11-5px w-full px-2.5 py-2 text-left leading-tight font-semibold transition-[filter] hover:brightness-95',
            done && 'opacity-60',
          )}
          style={{ background: macroTint[dominantMacro([meal])] }}
        >
          <span className={cn('block truncate', done && 'line-through')}>{name}</span>
          <span className="text-text-2 tnum block font-medium">
            {formatNumber(num(meal.calories))} kcal
          </span>
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="min-w-[150px]">
        {!done && (
          <DropdownMenuItem
            onSelect={() => {
              onCompleteMeal(meal);
            }}
          >
            <Check className="text-good size-4" />
            {t('calendar.meal_actions.mark_done')}
          </DropdownMenuItem>
        )}
        <DropdownMenuItem
          onSelect={() => {
            onOverrideMeal(meal.id);
          }}
        >
          <Sparkles className="size-4" />
          {t('calendar.meal_actions.override')}
        </DropdownMenuItem>
        {meal.status !== 'Planned' && (
          <DropdownMenuItem
            onSelect={() => {
              onResetMeal(meal);
            }}
          >
            <RotateCcw className="size-4" />
            {t('calendar.meal_actions.reset')}
          </DropdownMenuItem>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onSelect={() => {
            onEditMeal(meal);
          }}
        >
          <Pencil className="size-4" />
          {t('common.edit')}
        </DropdownMenuItem>
        <DropdownMenuItem
          onSelect={() => {
            onDeleteMeal(meal);
          }}
          className="text-destructive focus:text-destructive"
        >
          <Trash2 className="size-4" />
          {t('common.delete')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function LegendSwatch({ macro, label }: { macro: Macro; label: string }) {
  return (
    <span className="text-muted-foreground inline-flex items-center gap-1.5">
      <span className="size-2.5 rounded-full" style={{ background: `var(--color-${macro})` }} />
      {label}
    </span>
  );
}
