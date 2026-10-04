import {
  Card,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Skeleton,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { goalStatus } from '@shared/lib/format';
import { cn } from '@shared/lib/utils';
import {
  ArrowUp,
  Check,
  CheckCheck,
  Pencil,
  Plus,
  RotateCcw,
  Sparkles,
  Trash2,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { MealEntryDto } from '@modules/diet-planner/api/hooks/useMeals';

interface Slot {
  id: string;
  name: string;
  sortOrder: number | string;
  /** Default time of day, `HH:mm[:ss]`; shown under the slot name. */
  defaultTime?: string;
}

interface WeekGridProps {
  weekDays: Date[];
  slots: Slot[];
  meals: MealEntryDto[];
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
  /** Add, edit, delete controls (default true). */
  canPlan?: boolean;
  /** Complete, override, reset controls (default true). */
  canLog?: boolean;
  /** Label each meal with its person — set when viewing someone else's plan. */
  showPerson?: boolean;
}

/** `eaten` is solid, `next` is outlined, `missed` is a past planned meal never logged, `planned` is dashed. */
export type MealCellState = 'eaten' | 'next' | 'missed' | 'planned';

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

function timeOf(meal: MealEntryDto): string {
  return (meal.mealTime ?? meal.mealSlotDefaultTime).slice(0, 5);
}

/** The earliest meal of today that is still planned — the same one the Today screen emphasises. */
function findNextMeal(meals: MealEntryDto[], todayStr: string): MealEntryDto | undefined {
  return meals
    .filter((m) => m.date.slice(0, 10) === todayStr && m.status === 'Planned')
    .sort(
      (a, b) =>
        timeOf(a).localeCompare(timeOf(b)) ||
        num(a.mealSlotSortOrder) - num(b.mealSlotSortOrder) ||
        a.id.localeCompare(b.id),
    )[0];
}

function stateOf(meal: MealEntryDto, todayStr: string, nextId: string | undefined): MealCellState {
  if (isDone(meal)) return 'eaten';
  if (meal.id === nextId) return 'next';
  return meal.date.slice(0, 10) < todayStr ? 'missed' : 'planned';
}

export function WeekGrid({
  weekDays,
  slots,
  meals,
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
  canPlan = true,
  canLog = true,
  showPerson = false,
}: WeekGridProps) {
  const { t, i18n } = useTranslation();
  const fmt = useFormat();
  const todayStr = toDateStr(new Date());
  const nextMeal = findNextMeal(meals, todayStr);

  const byDaySlot = new Map<string, MealEntryDto[]>();
  const kcalByDate = new Map<string, number>();
  for (const meal of meals) {
    const date = meal.date.slice(0, 10);
    const key = `${date}|${meal.mealSlotId}`;
    const list = byDaySlot.get(key) ?? [];
    list.push(meal);
    byDaySlot.set(key, list);
    kcalByDate.set(date, (kcalByDate.get(date) ?? 0) + num(meal.calories));
  }

  const columns = `112px repeat(${String(weekDays.length)}, minmax(0, 1fr))`;

  if (loading) {
    return (
      <Card className="p-4">
        <Skeleton className="h-420px w-full" />
      </Card>
    );
  }

  return (
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
              className={cn('border-border border-b px-3 py-3 text-left', isToday && 'bg-accent')}
            >
              <div className={cn('text-12px font-bold', isToday && 'text-primary')}>
                {date.toLocaleDateString(i18n.language, { weekday: 'short' })}
              </div>
              <div className={cn('text-11px', isToday ? 'text-primary' : 'text-muted-foreground')}>
                {date.toLocaleDateString(i18n.language, { day: 'numeric', month: 'short' })}
                {isToday ? ` · ${t('calendar.week_grid.today')}` : ''}
              </div>
            </div>
          );
        })}

        {/* Slot rows */}
        {slots.map((slot) => (
          <SlotRow
            key={slot.id}
            slot={slot}
            weekDays={weekDays}
            todayStr={todayStr}
            nextId={nextMeal?.id}
            byDaySlot={byDaySlot}
            onAddMeal={onAddMeal}
            onEditMeal={onEditMeal}
            onCompleteMeal={onCompleteMeal}
            onResetMeal={onResetMeal}
            onOverrideMeal={onOverrideMeal}
            onDeleteMeal={onDeleteMeal}
            access={{ canPlan, canLog, showPerson }}
          />
        ))}

        {/* Day total row */}
        <div className="border-border text-12px border-t px-3 py-3 font-bold">
          {t('calendar.week_grid.day_total')}
        </div>
        {weekDays.map((date) => {
          const dateStr = toDateStr(date);
          const isToday = dateStr === todayStr;
          const kcal = Math.round(kcalByDate.get(dateStr) ?? 0);
          const status = kcal > 0 ? goalStatus(kcal, calorieTarget, 'limit') : null;
          const hasPlanned = meals.some(
            (m) => m.date.slice(0, 10) === dateStr && m.status === 'Planned',
          );
          return (
            <div
              key={dateStr}
              className={cn('border-border relative border-t px-3 py-3', isToday && 'bg-accent')}
            >
              <div className="numeral text-12-5px font-bold">
                {kcal > 0 ? fmt.energy(kcal) : '–'}
              </div>
              {status === null || status.state === 'none' ? null : (
                <div
                  className={cn(
                    'text-11px',
                    status.state === 'over' &&
                      'text-over inline-flex items-center gap-0.5 font-bold',
                    status.state === 'onTarget' && 'text-good',
                    status.state === 'under' && 'text-muted-foreground',
                  )}
                >
                  {status.state === 'over' && <ArrowUp className="size-3" strokeWidth={3} />}
                  {t(`calendar.week_grid.total_${status.state}`, {
                    amount: fmt.energy(Math.abs(status.diff)),
                  })}
                </div>
              )}
              {hasPlanned && canLog ? (
                <button
                  type="button"
                  onClick={() => {
                    onBulkComplete(dateStr);
                  }}
                  disabled={bulkPending}
                  title={t('calendar.bulk_complete.button')}
                  aria-label={t('calendar.bulk_complete.button')}
                  className="text-muted-foreground hover:text-good absolute top-1.5 right-1.5 grid size-6 place-items-center rounded transition-colors"
                >
                  <CheckCheck className="size-3.5" />
                </button>
              ) : null}
            </div>
          );
        })}
      </div>
    </Card>
  );
}

interface ChipAccess {
  canPlan: boolean;
  canLog: boolean;
  showPerson: boolean;
}

interface SlotRowProps {
  slot: Slot;
  weekDays: Date[];
  todayStr: string;
  nextId: string | undefined;
  byDaySlot: Map<string, MealEntryDto[]>;
  onAddMeal: (date: string, slotId: string) => void;
  onEditMeal: (meal: MealEntryDto) => void;
  onCompleteMeal: (meal: MealEntryDto) => void;
  onResetMeal: (meal: MealEntryDto) => void;
  onOverrideMeal: (mealId: string) => void;
  onDeleteMeal: (meal: MealEntryDto) => void;
  access: ChipAccess;
}

function SlotRow({
  slot,
  weekDays,
  todayStr,
  nextId,
  byDaySlot,
  onAddMeal,
  onEditMeal,
  onCompleteMeal,
  onResetMeal,
  onOverrideMeal,
  onDeleteMeal,
  access,
}: SlotRowProps) {
  const { t, i18n } = useTranslation();
  return (
    <>
      <div role="rowheader" className="border-border text-12px border-t px-3 py-3">
        <div className="font-semibold">{slot.name}</div>
        {slot.defaultTime ? (
          <div className="text-muted-foreground text-11px">{slot.defaultTime.slice(0, 5)}</div>
        ) : null}
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
            className={cn('border-border border-t p-1.5', isToday && 'bg-accent')}
          >
            {cellMeals.length === 0 ? (
              access.canPlan && (
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
              )
            ) : (
              <div className="flex flex-col gap-1.5">
                {cellMeals.map((meal) => (
                  <MealChip
                    key={meal.id}
                    meal={meal}
                    state={stateOf(meal, todayStr, nextId)}
                    onEditMeal={onEditMeal}
                    onCompleteMeal={onCompleteMeal}
                    onResetMeal={onResetMeal}
                    onOverrideMeal={onOverrideMeal}
                    onDeleteMeal={onDeleteMeal}
                    access={access}
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
  state: MealCellState;
  onEditMeal: (meal: MealEntryDto) => void;
  onCompleteMeal: (meal: MealEntryDto) => void;
  onResetMeal: (meal: MealEntryDto) => void;
  onOverrideMeal: (mealId: string) => void;
  onDeleteMeal: (meal: MealEntryDto) => void;
  access: ChipAccess;
}

const chipStateClass: Record<MealCellState, string> = {
  eaten: 'border-border-strong bg-card border',
  next: 'border-primary bg-card border-[1.5px]',
  missed: 'bg-warning-soft border-warning/30 border',
  planned: 'border-border-strong bg-card border border-dashed',
};

function MealChip({
  meal,
  state,
  onEditMeal,
  onCompleteMeal,
  onResetMeal,
  onOverrideMeal,
  onDeleteMeal,
  access: { canPlan, canLog, showPerson },
}: MealChipProps) {
  const { t } = useTranslation();
  const fmt = useFormat();
  const done = state === 'eaten';
  const name =
    meal.status === 'Modified' && meal.actualRecipe ? meal.actualRecipe.name : meal.recipeName;
  const chipClass = cn(
    'rounded-12px text-11-5px flex min-h-[76px] w-full flex-col justify-between gap-1 px-2.5 py-2 text-left leading-tight',
    chipStateClass[state],
  );
  const chipBody = (
    <>
      <span className="line-clamp-2 font-semibold break-words">{name}</span>
      {showPerson && meal.personName !== null && (
        <span className="text-text-2 block truncate font-medium">{meal.personName}</span>
      )}
      <span className="flex flex-col gap-0.5">
        {state === 'missed' && (
          <span className="text-warning text-11px font-bold">
            {t('calendar.week_grid.not_logged')}
          </span>
        )}
        <span className="text-text-2 tnum flex items-center gap-1 font-medium">
          {done && <Check className="text-good size-3.5" strokeWidth={3} />}
          {state === 'next' && (
            <span className="text-primary font-bold">{t('calendar.week_grid.next')} ·</span>
          )}
          {fmt.energy(num(meal.calories))}
        </span>
      </span>
    </>
  );

  if (!canPlan && !canLog) return <div className={chipClass}>{chipBody}</div>;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          className={cn(chipClass, 'hover:border-primary/60 transition-colors')}
        >
          {chipBody}
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="min-w-[150px]">
        {canLog && !done && (
          <DropdownMenuItem
            onSelect={() => {
              onCompleteMeal(meal);
            }}
          >
            <Check className="text-good size-4" />
            {t('calendar.meal_actions.mark_done')}
          </DropdownMenuItem>
        )}
        {canLog && (
          <DropdownMenuItem
            onSelect={() => {
              onOverrideMeal(meal.id);
            }}
          >
            <Sparkles className="size-4" />
            {t('calendar.meal_actions.override')}
          </DropdownMenuItem>
        )}
        {canLog && meal.status !== 'Planned' && (
          <DropdownMenuItem
            onSelect={() => {
              onResetMeal(meal);
            }}
          >
            <RotateCcw className="size-4" />
            {t('calendar.meal_actions.reset')}
          </DropdownMenuItem>
        )}
        {canPlan && (
          <>
            {canLog && <DropdownMenuSeparator />}
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
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
