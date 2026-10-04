import { useHousehold } from '@modules/household';
import {
  Banner,
  Select,
  Skeleton,
  Button,
  PageContainer,
  PageHeader,
  SegmentedControl,
} from '@shared/components/ui';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@shared/components/ui/Dialog';
import { useToast } from '@shared/context/ToastContext';
import { useFormat } from '@shared/hooks/useFormat';
import { usePreferences } from '@shared/hooks/usePreferences';
import { ChevronLeft, ChevronRight, Plus, Upload } from 'lucide-react';
import { useState, useRef, useSyncExternalStore } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';

import { useGoals } from '../api/hooks/useGoals';
import { useMealSchedule } from '../api/hooks/useMealSchedule';
import {
  useMeals,
  useCreateMeal,
  useUpdateMeal,
  useDeleteMeal,
  useCompleteMeal,
  useResetMeal,
  useBulkCompleteMeals,
} from '../api/hooks/useMeals';
import { WeekGrid } from '../components/calendar/WeekGrid';
import { WeekMeters, type WeekTotals } from '../components/calendar/WeekMeters';
import { DayView } from '../components/calendar-day/DayView';
import { MealForm } from '../components/diet-plans/MealForm';
import { MealOverrideDialog } from '../components/diet-plans/MealOverrideDialog';
import { mealAccess, plannableMembers } from '../utils/householdAccess';

function getWeekStart(date: Date, weekStart: 'monday' | 'sunday') {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  const day = d.getDay();
  const diff =
    weekStart === 'sunday' ? d.getDate() - day : d.getDate() - day + (day === 0 ? -6 : 1);
  d.setDate(diff);
  return d;
}

function formatLocalDate(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${String(year)}-${month}-${day}`;
}

const ISO_DATE_RE = /^\d{4}-\d{2}-\d{2}$/;

function parseLocalDate(value: string | null): Date | null {
  if (value === null || !ISO_DATE_RE.test(value)) return null;
  const d = new Date(`${value}T00:00:00`);
  if (Number.isNaN(d.getTime())) return null;
  d.setHours(0, 0, 0, 0);
  return d;
}

function resolveSelectedDay(dateParam: string | null): Date {
  const parsed = parseLocalDate(dateParam);
  if (parsed !== null) return parsed;
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return today;
}

const EMPTY_TOTALS: WeekTotals = { calories: 0, protein: 0, carbs: 0, fat: 0, fiber: 0 };

const narrowQuery = '(max-width: 767px)';
function subscribeViewport(callback: () => void) {
  const query = window.matchMedia(narrowQuery);
  query.addEventListener('change', callback);
  return () => {
    query.removeEventListener('change', callback);
  };
}
function isNarrowViewport() {
  return window.matchMedia(narrowQuery).matches;
}

type CalendarView = 'week' | 'day';

interface Meal {
  id: string;
  date: string;
  mealSlotId: string;
  mealSlotName: string;
  mealSlotSortOrder: number;
  recipeId: string;
  recipeName: string;
  servings: number | string;
  notes?: string;
  status?: string;
  actualRecipe?: { id: string; name: string } | null;
  actualProducts?: { id: string; productName: string }[];
}

export default function Calendar() {
  const { t, i18n } = useTranslation();
  const fmt = useFormat();
  const toast = useToast();
  const [searchParams, setSearchParams] = useSearchParams();
  const dateHeadingRef = useRef<HTMLHeadingElement>(null);

  const narrow = useSyncExternalStore(subscribeViewport, isNarrowViewport, () => false);
  const explicitView = searchParams.get('view');
  const view: CalendarView =
    explicitView === 'day' || explicitView === 'week' ? explicitView : narrow ? 'day' : 'week';

  // Single shared anchor — interpreted as the selected day in day view, or any
  // day within the shown week in week view. Toggling views keeps you on the
  // same date so week ↔ day navigation feels continuous.
  const selectedDay = resolveSelectedDay(searchParams.get('date'));

  const { prefs } = usePreferences();
  const weekStart = getWeekStart(selectedDay, prefs.weekStart);

  function updateParams(patch: Record<string, string | null>, options: { replace?: boolean } = {}) {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        for (const [key, value] of Object.entries(patch)) {
          if (value === null) next.delete(key);
          else next.set(key, value);
        }
        return next;
      },
      { replace: options.replace ?? true },
    );
  }

  function setView(next: CalendarView) {
    // View switch is a meaningful navigation step — push so back returns to the previous view.
    updateParams({ view: next }, { replace: false });
  }

  // Whose plan is shown: `?person=<id>` for another household member, absent = the caller.
  // From the household sync, not the diet profile — a caller without a profile still has a person id.
  const { members, myRole, myPersonId: callerPersonId } = useHousehold();
  const myPersonId = callerPersonId ?? undefined;
  const personParam = searchParams.get('person');
  const viewed = members.find((m) => m.personId === personParam && m.personId !== myPersonId);
  const personId = viewed?.personId;
  const { canPlan, canLog } = mealAccess(myRole, myPersonId, viewed);
  const assignees = plannableMembers(members, myRole, myPersonId).map((m) => ({
    value: m.personId === myPersonId ? '' : m.personId,
    name: m.displayName,
  }));

  const [mealFormOpen, setMealFormOpen] = useState(false);
  const [mealFormDate, setMealFormDate] = useState('');
  const [mealFormSlotId, setMealFormSlotId] = useState('');
  const [editingMeal, setEditingMeal] = useState<Meal | null>(null);
  const [deletingMeal, setDeletingMeal] = useState<Meal | null>(null);

  const { data: schedule } = useMealSchedule(personId);
  const slots = [...(schedule?.slots ?? [])].sort(
    (a, b) => Number(a.sortOrder) - Number(b.sortOrder),
  );

  const createMeal = useCreateMeal();
  const updateMeal = useUpdateMeal();
  const deleteMeal = useDeleteMeal();
  const completeMeal = useCompleteMeal();
  const resetMeal = useResetMeal();
  const bulkCompleteMeals = useBulkCompleteMeals();
  const [overrideMealId, setOverrideMealId] = useState<string | null>(null);

  const weekStartDate = new Date(weekStart);
  const weekEndDate = new Date(weekStartDate);
  weekEndDate.setDate(weekEndDate.getDate() + 6);
  const weekRange = {
    from: formatLocalDate(weekStartDate),
    to: formatLocalDate(weekEndDate),
  };

  const { data: goals } = useGoals();

  const { data: meals, isLoading: mealsLoading } = useMeals({
    from: weekRange.from,
    to: weekRange.to,
    ...(personId !== undefined ? { personId } : {}),
  });

  // Everything planned for the week, eaten or not (meal quantities already include overrides).
  const weeklyTotals = (meals ?? []).reduce<WeekTotals>(
    (acc, meal) => ({
      calories: acc.calories + Number(meal.calories),
      protein: acc.protein + Number(meal.protein),
      carbs: acc.carbs + Number(meal.carbs),
      fat: acc.fat + Number(meal.fat),
      fiber: acc.fiber + Number(meal.fiber),
    }),
    EMPTY_TOTALS,
  );

  const weekDays = Array.from({ length: 7 }, (_, i) => {
    const d = new Date(weekStartDate);
    d.setDate(d.getDate() + i);
    return d;
  });

  const openCreateForm = (date: string, slotId: string) => {
    setEditingMeal(null);
    setMealFormDate(date);
    setMealFormSlotId(slotId);
    setMealFormOpen(true);
  };

  const openEditForm = (meal: Meal) => {
    setEditingMeal(meal);
    setMealFormOpen(true);
  };

  const handleFormSubmit = async (data: {
    personId: string | null;
    date: string;
    mealSlotId: string;
    recipeId: string;
    servings: number;
    notes: string;
  }) => {
    const { personId: assignee, ...fields } = data;
    const payload = { ...fields, mealTime: null, sequenceOrder: null };
    try {
      if (editingMeal) {
        await updateMeal.mutateAsync({ id: editingMeal.id, data: payload });
        toast.success(t('meal_form.edit_success'));
      } else {
        await createMeal.mutateAsync({ ...payload, personId: assignee });
        toast.success(t('meal_form.add_success'));
      }
      setMealFormOpen(false);
      setEditingMeal(null);
    } catch {
      toast.error(editingMeal ? t('meal_form.edit_error') : t('meal_form.add_error'));
    }
  };

  const handleDelete = async () => {
    if (!deletingMeal) return;
    try {
      await deleteMeal.mutateAsync(deletingMeal.id);
      toast.success(t('meal_form.delete_success'));
      setDeletingMeal(null);
    } catch {
      toast.error(t('meal_form.delete_error'));
    }
  };

  const handleComplete = async (meal: Meal) => {
    try {
      await completeMeal.mutateAsync(meal.id);
      toast.success(t('calendar.meal_action_success.done'));
    } catch {
      toast.error(t('calendar.meal_action_error.done'));
    }
  };

  const handleReset = async (meal: Meal) => {
    try {
      await resetMeal.mutateAsync(meal.id);
      toast.success(t('calendar.meal_action_success.reset'));
    } catch {
      toast.error(t('calendar.meal_action_error.reset'));
    }
  };

  const handleBulkComplete = async (date: string) => {
    try {
      const result = await bulkCompleteMeals.mutateAsync({ date, personId });
      const completed = Number(result.completed);
      if (completed > 0) {
        toast.success(t('calendar.bulk_complete.success', { count: completed }));
      }
    } catch {
      toast.error(t('calendar.bulk_complete.error'));
    }
  };

  const isSubmitting = createMeal.isPending || updateMeal.isPending;

  const shiftDate = (days: number) => {
    const d = new Date(selectedDay);
    d.setDate(d.getDate() + days);
    updateParams({ date: formatLocalDate(d) });
  };

  const goToPrevWeek = () => {
    shiftDate(-7);
  };
  const goToNextWeek = () => {
    shiftDate(7);
  };
  const goToPrevDay = () => {
    shiftDate(-1);
  };
  const goToNextDay = () => {
    shiftDate(1);
  };
  const goToToday = () => {
    updateParams({ date: null }, { replace: false });
    dateHeadingRef.current?.focus();
    dateHeadingRef.current?.scrollIntoView({ block: 'nearest' });
  };

  const dayDateStr = formatLocalDate(selectedDay);
  const dayRange = view === 'day' ? { from: dayDateStr, to: dayDateStr } : weekRange;
  const dayQuery = useMeals({
    from: dayRange.from,
    to: dayRange.to,
    ...(personId !== undefined ? { personId } : {}),
  });
  const dayMeals = dayQuery.data;

  const rangeLabel = fmt.dayRange(weekDays[0], weekDays[6]);
  const calorieTarget = viewed ? null : (goals?.dailyCalorieTarget ?? null);
  const firstSlot = slots[0];

  return (
    <PageContainer className="animate-fade-in-up">
      <PageHeader
        title={t('calendar.title')}
        subtitle={
          calorieTarget === null
            ? rangeLabel
            : t('calendar.subtitle_target', { range: rangeLabel, kcal: fmt.energy(calorieTarget) })
        }
        actions={
          <>
            <Button variant="outline" size="xl" asChild>
              <Link to="/diet-planner/import">
                <Upload className="size-4" />
                {t('calendar.import')}
              </Link>
            </Button>
            {canPlan && firstSlot ? (
              <Button
                size="xl"
                onClick={() => {
                  openCreateForm(formatLocalDate(new Date()), firstSlot.id);
                }}
              >
                <Plus className="size-4" />
                {t('calendar.add_meal')}
              </Button>
            ) : null}
          </>
        }
      />
      <>
        {/* Controls */}
        <h2 ref={dateHeadingRef} tabIndex={-1} className="sr-only">
          {view === 'week'
            ? t('diet_plan_detail.week_of', {
                date:
                  weekDays[0]?.toLocaleDateString(i18n.language, {
                    month: 'long',
                    day: 'numeric',
                  }) ?? '',
              })
            : selectedDay.toLocaleDateString(i18n.language, {
                weekday: 'long',
                month: 'long',
                day: 'numeric',
              })}
        </h2>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2.5">
            {members.length > 1 && (
              <div className="flex items-center gap-2">
                <span aria-hidden="true" className="text-muted-foreground text-sm">
                  {t('calendar.person_for')}
                </span>
                <div className="w-44">
                  <Select
                    aria-label={t('calendar.person_filter')}
                    value={personId ?? myPersonId ?? ''}
                    onChange={(e) => {
                      const next = e.target.value;
                      updateParams(
                        { person: next === myPersonId ? null : next },
                        { replace: false },
                      );
                    }}
                  >
                    {/* Every member is readable; a null role only reuses the caller-first order. */}
                    {plannableMembers(members, null, myPersonId).map((m) => (
                      <option key={m.personId} value={m.personId}>
                        {m.displayName}
                      </option>
                    ))}
                  </Select>
                </div>
              </div>
            )}
            <SegmentedControl
              label={t('calendar.view_label')}
              value={view}
              onChange={setView}
              options={[
                { value: 'day', label: t('calendar.day_view') },
                { value: 'week', label: t('calendar.week_view') },
              ]}
            />
          </div>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={view === 'week' ? goToPrevWeek : goToPrevDay}
              aria-label={t('common.previous')}
              className="border-border bg-card text-text-2 hover:border-border-strong hover:text-foreground rounded-12px grid size-11 place-items-center border transition-colors"
            >
              <ChevronLeft className="size-4" />
            </button>
            <Button variant="outline" className="min-h-11" onClick={goToToday}>
              {t(view === 'week' ? 'calendar.this_week' : 'calendar.today')}
            </Button>
            <button
              type="button"
              onClick={view === 'week' ? goToNextWeek : goToNextDay}
              aria-label={t('common.next')}
              className="border-border bg-card text-text-2 hover:border-border-strong hover:text-foreground rounded-12px grid size-11 place-items-center border transition-colors"
            >
              <ChevronRight className="size-4" />
            </button>
          </div>
        </div>

        {view === 'day' && (
          <div
            className="mb-5 grid grid-cols-7 gap-1"
            role="group"
            aria-label={t('calendar.choose_day')}
          >
            {weekDays.map((day) => (
              <Button
                key={formatLocalDate(day)}
                variant={formatLocalDate(day) === dayDateStr ? 'default' : 'outline'}
                className="h-auto min-h-14 min-w-0 flex-col px-1"
                aria-pressed={formatLocalDate(day) === dayDateStr}
                aria-label={day.toLocaleDateString(i18n.language, {
                  weekday: 'long',
                  month: 'long',
                  day: 'numeric',
                })}
                onClick={() => {
                  updateParams({ date: formatLocalDate(day) }, { replace: false });
                }}
              >
                <span className="text-xs">
                  {day.toLocaleDateString(i18n.language, { weekday: 'short' })}
                </span>
                <span>{day.getDate()}</span>
              </Button>
            ))}
          </div>
        )}
        {view === 'day' &&
          (dayQuery.isError ? (
            <Banner
              variant="error"
              onRetry={() => {
                void dayQuery.refetch();
              }}
              retryLabel={t('dashboard.retry')}
            >
              {t('dashboard.data_error')}
            </Banner>
          ) : dayQuery.isPending || dayQuery.isPlaceholderData ? (
            <Skeleton className="h-72 w-full" />
          ) : (
            <DayView
              date={dayDateStr}
              slots={slots.map((s) => ({
                id: s.id,
                name: s.name,
                defaultTime: s.defaultTime,
                sortOrder: s.sortOrder,
              }))}
              meals={dayMeals ?? []}
              onAddMeal={(slotId) => {
                openCreateForm(dayDateStr, slotId);
              }}
              onEditMeal={(meal) => {
                openEditForm(meal as unknown as Meal);
              }}
              onDeleteMeal={(meal) => {
                setDeletingMeal(meal as unknown as Meal);
              }}
              onCompleteMeal={(meal) => void handleComplete(meal as unknown as Meal)}
              onResetMeal={(meal) => void handleReset(meal as unknown as Meal)}
              onOverrideMeal={(mealId) => {
                setOverrideMealId(mealId);
              }}
              canPlan={canPlan}
              canLog={canLog}
              otherPerson={viewed !== undefined}
            />
          ))}

        {view === 'week' ? (
          <WeekGrid
            weekDays={weekDays}
            slots={slots.map((s) => ({
              id: s.id,
              name: s.name,
              sortOrder: s.sortOrder,
              defaultTime: s.defaultTime,
            }))}
            meals={meals ?? []}
            calorieTarget={calorieTarget}
            loading={mealsLoading}
            bulkPending={bulkCompleteMeals.isPending}
            onAddMeal={openCreateForm}
            onEditMeal={(meal) => {
              openEditForm(meal as unknown as Meal);
            }}
            onCompleteMeal={(meal) => void handleComplete(meal as unknown as Meal)}
            onResetMeal={(meal) => void handleReset(meal as unknown as Meal)}
            onOverrideMeal={(mealId) => {
              setOverrideMealId(mealId);
            }}
            onDeleteMeal={(meal) => {
              setDeletingMeal(meal as unknown as Meal);
            }}
            onBulkComplete={(date) => void handleBulkComplete(date)}
            canPlan={canPlan}
            canLog={canLog}
            showPerson={viewed !== undefined}
          />
        ) : null}

        {view === 'week' && !viewed && <WeekMeters totals={weeklyTotals} goals={goals ?? null} />}

        <MealForm
          key={`${editingMeal?.id ?? 'new'}-${String(mealFormOpen)}`}
          open={mealFormOpen}
          onClose={() => {
            setMealFormOpen(false);
            setEditingMeal(null);
          }}
          onSubmit={(data) => {
            void handleFormSubmit(data);
          }}
          initialDate={editingMeal ? undefined : mealFormDate}
          initialMealSlotId={editingMeal ? undefined : mealFormSlotId}
          initialValues={
            editingMeal
              ? {
                  date: editingMeal.date,
                  mealSlotId: editingMeal.mealSlotId,
                  recipeId: editingMeal.recipeId,
                  recipeName: editingMeal.recipeName,
                  servings: Number(editingMeal.servings),
                  notes: editingMeal.notes ?? '',
                }
              : undefined
          }
          assignees={assignees}
          initialPersonId={personId ?? ''}
          isSubmitting={isSubmitting}
          mode={editingMeal ? 'edit' : 'create'}
        />

        {overrideMealId !== null && (
          <MealOverrideDialog
            open
            onClose={() => {
              setOverrideMealId(null);
            }}
            mealEntryId={overrideMealId}
          />
        )}

        {/* Delete confirmation dialog */}
        <Dialog
          open={!!deletingMeal}
          onOpenChange={(v) => {
            if (!v) setDeletingMeal(null);
          }}
        >
          <DialogContent className="max-w-sm">
            <DialogHeader>
              <DialogTitle>{t('meal_form.delete_title')}</DialogTitle>
            </DialogHeader>
            <DialogDescription>
              {t('meal_form.delete_description', { name: deletingMeal?.recipeName ?? '' })}
            </DialogDescription>
            <DialogFooter>
              <Button
                variant="outline"
                onClick={() => {
                  setDeletingMeal(null);
                }}
                disabled={deleteMeal.isPending}
              >
                {t('common.cancel')}
              </Button>
              <Button
                variant="destructive"
                onClick={() => {
                  void handleDelete();
                }}
                disabled={deleteMeal.isPending}
              >
                {deleteMeal.isPending ? t('common.deleting') : t('common.delete')}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </>
    </PageContainer>
  );
}
