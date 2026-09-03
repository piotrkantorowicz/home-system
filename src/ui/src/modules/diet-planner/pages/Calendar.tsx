import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
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
import { cn } from '@shared/lib/utils';
import {
  ArrowRight,
  Check,
  ChevronLeft,
  ChevronRight,
  MoreVertical,
  Pencil,
  Plus,
  RotateCcw,
  Sparkles,
  Target,
  Trash2,
} from 'lucide-react';
import { lazy, Suspense, useCallback, useState, useMemo } from 'react';
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
} from '../api/hooks/useMeals';
import { CalendarTabBar, type CalendarTab } from '../components/CalendarTabBar';
import { HydrationQuickAdd } from '../components/HydrationQuickAdd';
import { MacroProgressBar } from '../components/MacroProgressBar';
import { DayView } from '../components/calendar-day/DayView';
import { MealForm } from '../components/diet-plans/MealForm';
import { MealOverrideDialog } from '../components/diet-plans/MealOverrideDialog';
import { MealStatusBadge, type MealStatus } from '../components/diet-plans/MealStatusBadge';

const NutritionSummaryPage = lazy(() => import('./NutritionSummary'));
const ShoppingListPage = lazy(() => import('./ShoppingList'));
const ImportWizardPage = lazy(() => import('./diet-plans/ImportWizard'));

function getWeekStart(date: Date) {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  const day = d.getDay();
  const diff = d.getDate() - day + (day === 0 ? -6 : 1); // Monday start
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

  const view: CalendarView = searchParams.get('view') === 'day' ? 'day' : 'week';

  // Single shared anchor — interpreted as the selected day in day view, or any
  // day within the shown week in week view. Toggling views keeps you on the
  // same date so week ↔ day navigation feels continuous.
  const selectedDay = useMemo(() => {
    const parsed = parseLocalDate(searchParams.get('date'));
    if (parsed !== null) return parsed;
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return today;
  }, [searchParams]);

  const weekStart = useMemo(() => getWeekStart(selectedDay), [selectedDay]);

  const updateParams = useCallback(
    (patch: Record<string, string | null>, options: { replace?: boolean } = {}) => {
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
    },
    [setSearchParams],
  );

  const setView = useCallback(
    (next: CalendarView) => {
      // View switch is a meaningful navigation step — push so back returns to the previous view.
      updateParams({ view: next }, { replace: false });
    },
    [updateParams],
  );

  const [mealFormOpen, setMealFormOpen] = useState(false);
  const [mealFormDate, setMealFormDate] = useState('');
  const [mealFormSlotId, setMealFormSlotId] = useState('');
  const [editingMeal, setEditingMeal] = useState<Meal | null>(null);
  const [deletingMeal, setDeletingMeal] = useState<Meal | null>(null);

  const { data: schedule } = useMealSchedule();
  const slots = useMemo(
    () => [...(schedule?.slots ?? [])].sort((a, b) => Number(a.sortOrder) - Number(b.sortOrder)),
    [schedule],
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

  const weeklyTotals = useMemo(() => {
    if (!nutritionSummary) return { calories: 0, protein: 0, carbs: 0, fat: 0, fiber: 0 };
    return nutritionSummary.reduce(
      (acc, day) => ({
        calories: acc.calories + day.calories,
        protein: acc.protein + day.protein,
        carbs: acc.carbs + day.carbs,
        fat: acc.fat + day.fat,
        fiber: acc.fiber + day.fiber,
      }),
      { calories: 0, protein: 0, carbs: 0, fat: 0, fiber: 0 },
    );
  }, [nutritionSummary]);

  const { data: meals, isLoading: mealsLoading } = useMeals({
    from: weekRange.from,
    to: weekRange.to,
  });

  const mealsByDay = useMemo(() => {
    if (!meals) return {};
    const grouped: Record<string, Record<string, Meal[]>> = {};
    (meals as Meal[]).forEach((meal) => {
      const date = meal.date;
      const dayBucket = (grouped[date] ??= {});
      (dayBucket[meal.mealSlotId] ??= []).push(meal);
    });
    return grouped;
  }, [meals]);

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
    updateParams({ date: null });
  };

  const dayDateStr = formatLocalDate(selectedDay);
  const dayRange = view === 'day' ? { from: dayDateStr, to: dayDateStr } : weekRange;
  const { data: dayMeals } = useMeals({ from: dayRange.from, to: dayRange.to });

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
              <h2 className="text-xl font-semibold">
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
            <div className="flex flex-wrap items-center gap-2">
              <div
                role="tablist"
                aria-label={t('calendar.view_label')}
                className="bg-muted/40 flex rounded-md p-0.5"
              >
                <button
                  type="button"
                  role="tab"
                  aria-selected={view === 'week'}
                  onClick={() => {
                    setView('week');
                  }}
                  className={cn(
                    'rounded px-3 py-1 text-xs font-medium transition-colors',
                    view === 'week'
                      ? 'bg-background text-foreground shadow-sm'
                      : 'text-muted-foreground hover:text-foreground',
                  )}
                >
                  {t('calendar.view.week')}
                </button>
                <button
                  type="button"
                  role="tab"
                  aria-selected={view === 'day'}
                  onClick={() => {
                    setView('day');
                  }}
                  className={cn(
                    'rounded px-3 py-1 text-xs font-medium transition-colors',
                    view === 'day'
                      ? 'bg-background text-foreground shadow-sm'
                      : 'text-muted-foreground hover:text-foreground',
                  )}
                >
                  {t('calendar.view.day')}
                </button>
              </div>
              <Button variant="outline" size="sm" onClick={goToToday}>
                {t('calendar.today')}
              </Button>
              <Button
                variant="outline"
                size="icon"
                onClick={view === 'week' ? goToPrevWeek : goToPrevDay}
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button
                variant="outline"
                size="icon"
                onClick={view === 'week' ? goToNextWeek : goToNextDay}
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>

          {view === 'day' && (
            <DayView
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
          )}

          {view === 'week' && mealsLoading ? (
            <div className="flex items-center justify-center py-16">
              <div className="text-muted-foreground text-lg">{t('common.loading')}</div>
            </div>
          ) : view === 'week' ? (
            <div className="stagger-children grid gap-4 lg:grid-cols-7">
              {weekDays.map((date) => {
                const dateStr = formatLocalDate(date);
                const dayMeals = mealsByDay[dateStr] ?? {};
                const isToday = dateStr === formatLocalDate(new Date());

                return (
                  <Card
                    key={dateStr}
                    className={cn(
                      'min-h-[400px] transition-all duration-200',
                      isToday && 'border-primary/50 shadow-primary/10 shadow-md',
                    )}
                  >
                    <CardHeader className="pb-3">
                      <CardTitle className="text-sm">
                        <div className="flex items-start justify-between gap-2">
                          <div className="flex flex-col gap-1">
                            <span className="text-muted-foreground text-xs tracking-wider uppercase">
                              {date.toLocaleDateString(i18n.language, { weekday: 'short' })}
                            </span>
                            <span className={cn('text-xl font-bold', isToday && 'text-primary')}>
                              {date.getDate()}
                            </span>
                          </div>
                          {Object.values(dayMeals)
                            .flat()
                            .some((m) => m.status === 'Planned') && (
                            <button
                              type="button"
                              onClick={() => void handleBulkComplete(dateStr)}
                              className="text-muted-foreground focus-visible:ring-primary rounded p-1 text-[10px] font-medium transition-colors hover:text-emerald-600 focus-visible:ring-2 focus-visible:outline-none"
                              title={t('calendar.bulk_complete.button')}
                              aria-label={t('calendar.bulk_complete.button')}
                              disabled={bulkCompleteMeals.isPending}
                            >
                              {t('calendar.bulk_complete.button')}
                            </button>
                          )}
                        </div>
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-3">
                      {slots.length === 0 ? (
                        <div className="text-muted-foreground/60 rounded-lg border border-dashed p-3 text-center text-xs">
                          {t('calendar.no_schedule')}
                        </div>
                      ) : null}
                      {slots.map((slot) => (
                        <div key={slot.id}>
                          <div className="mb-1.5 flex items-center justify-between">
                            <h4 className="text-muted-foreground text-[10px] font-semibold tracking-wider uppercase">
                              {slot.name}
                            </h4>
                            <button
                              type="button"
                              onClick={() => {
                                openCreateForm(dateStr, slot.id);
                              }}
                              className="text-muted-foreground/50 hover:text-primary h-4 w-4 rounded transition-colors"
                              title={t('meal_form.add_title')}
                            >
                              <Plus className="h-3 w-3" />
                            </button>
                          </div>
                          {dayMeals[slot.id]?.map((meal) => (
                            <div
                              key={meal.id}
                              className="group bg-muted/30 hover:bg-muted/50 mb-1.5 rounded-lg border p-2 text-xs transition-colors"
                            >
                              <div className="flex items-start justify-between gap-1">
                                <div className="flex min-w-0 flex-1 items-start gap-1.5">
                                  <MealStatusBadge
                                    status={(meal.status as MealStatus | undefined) ?? 'Planned'}
                                    className="mt-0.5 shrink-0"
                                  />
                                  <div className="min-w-0 flex-1">
                                    {meal.status === 'Modified' && meal.actualRecipe ? (
                                      <>
                                        <Link
                                          to={`/diet-planner/recipes/${meal.actualRecipe.id}`}
                                          title={meal.actualRecipe.name}
                                          className="line-clamp-2 block font-medium break-words hover:underline"
                                        >
                                          {meal.actualRecipe.name}
                                        </Link>
                                        <span
                                          title={meal.recipeName}
                                          className="text-muted-foreground/70 block truncate text-[10px] line-through"
                                        >
                                          {meal.recipeName}
                                        </span>
                                      </>
                                    ) : (
                                      <Link
                                        to={`/diet-planner/recipes/${meal.recipeId}`}
                                        title={meal.recipeName}
                                        className="line-clamp-2 block font-medium break-words hover:underline"
                                      >
                                        {meal.recipeName}
                                      </Link>
                                    )}
                                  </div>
                                </div>
                                <DropdownMenu>
                                  <DropdownMenuTrigger asChild>
                                    <button
                                      type="button"
                                      className="text-muted-foreground hover:text-foreground focus-visible:ring-primary shrink-0 rounded p-0.5 opacity-50 transition-opacity group-focus-within:opacity-100 group-hover:opacity-100 focus-visible:opacity-100 focus-visible:ring-2 focus-visible:outline-none [@media(hover:none)]:opacity-100"
                                      aria-label={t('calendar.meal_actions.menu')}
                                    >
                                      <MoreVertical className="h-3.5 w-3.5" />
                                    </button>
                                  </DropdownMenuTrigger>
                                  <DropdownMenuContent
                                    align="end"
                                    className="min-w-[140px] p-1 [&_[role=menuitem]]:gap-1.5 [&_[role=menuitem]]:px-2 [&_[role=menuitem]]:py-1 [&_[role=menuitem]]:text-xs [&_[role=menuitem]_svg]:size-3"
                                  >
                                    {meal.status !== 'Done' && meal.status !== 'Modified' && (
                                      <DropdownMenuItem onSelect={() => void handleComplete(meal)}>
                                        <Check className="text-emerald-600" />
                                        {t('calendar.meal_actions.mark_done')}
                                      </DropdownMenuItem>
                                    )}
                                    <DropdownMenuItem
                                      onSelect={() => {
                                        setOverrideMealId(meal.id);
                                      }}
                                    >
                                      <Sparkles className="text-amber-600" />
                                      {t('calendar.meal_actions.override')}
                                    </DropdownMenuItem>
                                    {meal.status !== 'Planned' && (
                                      <DropdownMenuItem onSelect={() => void handleReset(meal)}>
                                        <RotateCcw />
                                        {t('calendar.meal_actions.reset')}
                                      </DropdownMenuItem>
                                    )}
                                    <DropdownMenuSeparator />
                                    <DropdownMenuItem
                                      onSelect={() => {
                                        openEditForm(meal);
                                      }}
                                    >
                                      <Pencil />
                                      {t('common.edit')}
                                    </DropdownMenuItem>
                                    <DropdownMenuItem
                                      onSelect={() => {
                                        setDeletingMeal(meal);
                                      }}
                                      className="text-destructive focus:text-destructive"
                                    >
                                      <Trash2 />
                                      {t('common.delete')}
                                    </DropdownMenuItem>
                                  </DropdownMenuContent>
                                </DropdownMenu>
                              </div>
                              <p className="text-muted-foreground mt-0.5">
                                {t('recipes.servings', { count: Number(meal.servings) || 1 })}
                              </p>
                              {meal.notes && (
                                <p className="text-muted-foreground/70 mt-0.5 truncate">
                                  {meal.notes}
                                </p>
                              )}
                            </div>
                          )) ?? (
                            <div className="text-muted-foreground/60 rounded-lg border border-dashed p-2 text-center text-xs">
                              {t('diet_plan_detail.no_meal')}
                            </div>
                          )}
                        </div>
                      ))}
                    </CardContent>
                  </Card>
                );
              })}
            </div>
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
