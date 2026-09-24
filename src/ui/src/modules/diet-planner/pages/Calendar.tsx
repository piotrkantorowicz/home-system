import {
  Banner,
  Skeleton,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
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
import { usePreferences } from '@shared/hooks/usePreferences';
import { cn } from '@shared/lib/utils';
import { ArrowRight, ChevronLeft, ChevronRight, Target } from 'lucide-react';
import { lazy, Suspense, useState, useRef, useSyncExternalStore } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';

import { useGoals } from '../api/hooks/useGoals';
import { useMealSchedule } from '../api/hooks/useMealSchedule';
import {
  useMeals,
  useCreateMeal,
  useUpdateMeal,
  useDeleteMeal,
  useNutritionSummary,
  useCompleteMeal,
  useResetMeal,
  useBulkCompleteMeals,
  type DailyNutrition,
} from '../api/hooks/useMeals';
import { CalendarTabBar, type CalendarTab } from '../components/CalendarTabBar';
import { HydrationQuickAdd } from '../components/HydrationQuickAdd';
import { MacroProgressBar } from '../components/MacroProgressBar';
import { WeekGrid } from '../components/calendar/WeekGrid';
import { DayView } from '../components/calendar-day/DayView';
import { MealForm } from '../components/diet-plans/MealForm';
import { MealOverrideDialog } from '../components/diet-plans/MealOverrideDialog';

const NutritionSummaryPage = lazy(() => import('./NutritionSummary'));
const ShoppingListPage = lazy(() => import('./ShoppingList'));
const ImportWizardPage = lazy(() => import('./diet-plans/ImportWizard'));

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

const EMPTY_TOTALS = { calories: 0, protein: 0, carbs: 0, fat: 0, fiber: 0 };

function sumWeeklyTotals(days: DailyNutrition[] | undefined) {
  if (!days) return EMPTY_TOTALS;
  return days.reduce(
    (acc, day) => ({
      calories: acc.calories + day.calories,
      protein: acc.protein + day.protein,
      carbs: acc.carbs + day.carbs,
      fat: acc.fat + day.fat,
      fiber: acc.fiber + day.fiber,
    }),
    EMPTY_TOTALS,
  );
}

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
  const toast = useToast();
  const [activeTab, setActiveTab] = useState<CalendarTab>('calendar');
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

  const [mealFormOpen, setMealFormOpen] = useState(false);
  const [mealFormDate, setMealFormDate] = useState('');
  const [mealFormSlotId, setMealFormSlotId] = useState('');
  const [editingMeal, setEditingMeal] = useState<Meal | null>(null);
  const [deletingMeal, setDeletingMeal] = useState<Meal | null>(null);

  const { data: schedule } = useMealSchedule();
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

  const { data: nutritionSummary } = useNutritionSummary({
    from: weekRange.from,
    to: weekRange.to,
  });
  const { data: goals } = useGoals();

  const nutritionByDate = new Map<string, DailyNutrition>();
  for (const day of nutritionSummary ?? []) nutritionByDate.set(day.date.slice(0, 10), day);

  const weeklyTotals = sumWeeklyTotals(nutritionSummary);

  const { data: meals, isLoading: mealsLoading } = useMeals({
    from: weekRange.from,
    to: weekRange.to,
  });

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
    date: string;
    mealSlotId: string;
    recipeId: string;
    servings: number;
    notes: string;
  }) => {
    const payload = { ...data, mealTime: null, sequenceOrder: null };
    try {
      if (editingMeal) {
        await updateMeal.mutateAsync({ id: editingMeal.id, data: payload });
        toast.success(t('meal_form.edit_success'));
      } else {
        await createMeal.mutateAsync(payload);
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
      const result = await bulkCompleteMeals.mutateAsync(date);
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
    updateParams({ date: null, view: 'day' }, { replace: false });
    dateHeadingRef.current?.focus();
    dateHeadingRef.current?.scrollIntoView({ block: 'nearest' });
  };

  const dayDateStr = formatLocalDate(selectedDay);
  const dayRange = view === 'day' ? { from: dayDateStr, to: dayDateStr } : weekRange;
  const dayQuery = useMeals({ from: dayRange.from, to: dayRange.to });
  const dayMeals = dayQuery.data;

  return (
    <div
      className={cn(
        'animate-fade-in-up py-8 lg:py-10',
        // Calendar tab needs maximum horizontal real estate for the 7-day grid
        activeTab === 'calendar' ? 'px-4 lg:px-6' : 'px-8 lg:px-10',
      )}
    >
      <div className="mb-8">
        <h1 className="mb-3 text-4xl font-bold tracking-tight">{t('calendar.title')}</h1>
        <p className="text-muted-foreground text-lg">{t('calendar.subtitle')}</p>
      </div>

      {/* Tab Bar */}
      <CalendarTabBar activeTab={activeTab} onTabChange={setActiveTab} />

      {activeTab === 'calendar' && (
        <>
          {/* Navigator */}
          <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 ref={dateHeadingRef} tabIndex={-1} className="text-xl font-semibold">
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
            </div>
            <div className="flex flex-wrap items-center gap-2.5">
              <SegmentedControl
                label={t('calendar.view_label')}
                value={view}
                onChange={setView}
                options={[
                  { value: 'day', label: t('calendar.day_view') },
                  { value: 'week', label: t('calendar.week_view') },
                ]}
              />
              <Button variant="outline" className="min-h-11" onClick={goToToday}>
                {t('calendar.today')}
              </Button>
              <button
                type="button"
                onClick={view === 'week' ? goToPrevWeek : goToPrevDay}
                aria-label={t('common.previous')}
                className="border-border bg-card text-text-2 hover:border-border-strong hover:text-foreground rounded-12px grid size-11 place-items-center border transition-colors"
              >
                <ChevronLeft className="size-4" />
              </button>
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
              />
            ))}

          {view === 'week' ? (
            <WeekGrid
              weekDays={weekDays}
              slots={slots.map((s) => ({ id: s.id, name: s.name, sortOrder: s.sortOrder }))}
              meals={meals ?? []}
              nutritionByDate={nutritionByDate}
              calorieTarget={goals?.dailyCalorieTarget ?? null}
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
            />
          ) : null}

          {/* Weekly Nutrition Summary — hidden in day view (DayView has its own summary) */}
          {view === 'week' && (
            <Card className="animate-fade-in-up mt-6">
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-base">{t('nutrition_summary.title')}</CardTitle>
                  {goals !== undefined && goals !== null && (
                    <Link
                      to="/diet-planner/profile?section=goals"
                      className="text-muted-foreground hover:text-primary text-sm transition-colors"
                    >
                      {t('nutrition_summary.goals_edit')}
                    </Link>
                  )}
                </div>
              </CardHeader>
              <CardContent>
                {goals === undefined || goals === null ? (
                  <div className="flex flex-col items-center gap-3 rounded-xl border border-dashed py-8 text-center">
                    <div className="rounded-xl bg-orange-500/10 p-2.5">
                      <Target className="h-5 w-5 text-orange-600 dark:text-orange-400" />
                    </div>
                    <p className="text-muted-foreground text-sm">
                      {t('nutrition_summary.goals_cta_title')}
                    </p>
                    <Link
                      to="/diet-planner/profile?section=goals"
                      className="bg-primary text-primary-foreground hover:bg-primary/90 inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors"
                    >
                      {t('nutrition_summary.goals_cta_button')}
                      <ArrowRight className="h-4 w-4" />
                    </Link>
                  </div>
                ) : weeklyTotals.calories === 0 &&
                  weeklyTotals.protein === 0 &&
                  weeklyTotals.carbs === 0 ? (
                  <p className="text-muted-foreground text-sm">{t('nutrition_summary.no_meals')}</p>
                ) : (
                  <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
                    <MacroProgressBar
                      label={t('nutrition_summary.calories')}
                      actual={weeklyTotals.calories}
                      goal={goals.dailyCalorieTarget !== null ? goals.dailyCalorieTarget * 7 : null}
                      unit="kcal"
                      gradient="from-rose-500 to-orange-500"
                    />
                    <MacroProgressBar
                      label={t('nutrition_summary.protein')}
                      actual={weeklyTotals.protein}
                      goal={goals.proteinGrams !== null ? goals.proteinGrams * 7 : null}
                      gradient="from-blue-500 to-indigo-500"
                    />
                    <MacroProgressBar
                      label={t('nutrition_summary.carbs')}
                      actual={weeklyTotals.carbs}
                      goal={goals.carbsGrams !== null ? goals.carbsGrams * 7 : null}
                      gradient="from-emerald-500 to-teal-500"
                    />
                    <MacroProgressBar
                      label={t('nutrition_summary.fat')}
                      actual={weeklyTotals.fat}
                      goal={goals.fatGrams !== null ? goals.fatGrams * 7 : null}
                      gradient="from-amber-500 to-orange-500"
                    />
                    <MacroProgressBar
                      label={t('nutrition_summary.fiber')}
                      actual={weeklyTotals.fiber}
                      goal={goals.fiberGrams !== null ? goals.fiberGrams * 7 : null}
                      gradient="from-violet-500 to-purple-500"
                    />
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {/* Hydration Quick Add — week view only (DaySummaryCard already includes it) */}
          {view === 'week' && <HydrationQuickAdd />}

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
      )}

      {activeTab === 'nutrition' && (
        <Suspense
          fallback={
            <div className="text-muted-foreground flex items-center justify-center py-16">
              {t('common.loading')}
            </div>
          }
        >
          <NutritionSummaryPage />
        </Suspense>
      )}

      {activeTab === 'shopping' && (
        <Suspense
          fallback={
            <div className="text-muted-foreground flex items-center justify-center py-16">
              {t('common.loading')}
            </div>
          }
        >
          <ShoppingListPage />
        </Suspense>
      )}

      {activeTab === 'import' && (
        <Suspense
          fallback={
            <div className="text-muted-foreground flex items-center justify-center py-16">
              {t('common.loading')}
            </div>
          }
        >
          <ImportWizardPage />
        </Suspense>
      )}
    </div>
  );
}
