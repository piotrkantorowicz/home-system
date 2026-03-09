import { useState, useMemo, useEffect } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  Calendar as CalendarIcon,
  Plus,
  Pencil,
  Trash2,
} from 'lucide-react';
import {
  useDietPlan,
  useMeals,
  useCreateMeal,
  useUpdateMeal,
  useDeleteMeal,
} from '@modules/diet-planner/api/hooks/useDietPlans';
import { Button, Card, CardContent, CardHeader, CardTitle } from '@shared/components/ui';
import { Badge } from '@shared/components/ui/Badge';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@shared/components/ui/Dialog';
import { MealForm } from '@modules/diet-planner/components/diet-plans/MealForm';
import { cn } from '@shared/lib/utils';

function parseLocalDate(dateStr: string) {
  const [year, month, day] = dateStr.split('-').map(Number);
  return new Date(year, month - 1, day);
}

function formatLocalDate(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

type Meal = {
  id: string;
  date: string;
  mealType: string;
  recipeId: string;
  recipeName: string;
  servings: number | string;
  notes?: string;
};

export default function DietPlanDetail() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { data: plan, isLoading: planLoading } = useDietPlan(id!);
  const [selectedWeekStart, setSelectedWeekStart] = useState<Date>(new Date());

  const [mealFormOpen, setMealFormOpen] = useState(false);
  const [mealFormDate, setMealFormDate] = useState('');
  const [mealFormType, setMealFormType] = useState('breakfast');
  const [editingMeal, setEditingMeal] = useState<Meal | null>(null);
  const [deletingMeal, setDeletingMeal] = useState<Meal | null>(null);

  const createMeal = useCreateMeal(id!);
  const updateMeal = useUpdateMeal(id!);
  const deleteMeal = useDeleteMeal(id!);

  useEffect(() => {
    if (plan) {
      setSelectedWeekStart(parseLocalDate(plan.startDate));
    }
  }, [plan]);

  const weekRange = useMemo(() => {
    const start = new Date(selectedWeekStart);
    start.setHours(0, 0, 0, 0);
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    return { start: formatLocalDate(start), end: formatLocalDate(end) };
  }, [selectedWeekStart]);

  const { data: meals, isLoading: mealsLoading } = useMeals(id!, weekRange.start, weekRange.end);

  const mealsByDay = useMemo(() => {
    if (!meals) return {};
    const grouped: Record<string, Record<string, Meal[]>> = {};
    (meals as Meal[]).forEach((meal) => {
      const date = meal.date || '';
      const mealType = meal.mealType || 'other';
      if (!grouped[date]) grouped[date] = {};
      if (!grouped[date][mealType]) grouped[date][mealType] = [];
      grouped[date][mealType].push(meal);
    });
    return grouped;
  }, [meals]);

  const weekDays = useMemo(() => {
    const days = [];
    const start = new Date(selectedWeekStart);
    for (let i = 0; i < 7; i++) {
      const date = new Date(start);
      date.setDate(date.getDate() + i);
      days.push(date);
    }
    return days;
  }, [selectedWeekStart]);

  const openCreateForm = (date: string, mealType: string) => {
    setEditingMeal(null);
    setMealFormDate(date);
    setMealFormType(mealType);
    setMealFormOpen(true);
  };

  const openEditForm = (meal: Meal) => {
    setEditingMeal(meal);
    setMealFormOpen(true);
  };

  const handleFormSubmit = async (data: {
    date: string;
    mealType: string;
    recipeId: string;
    servings: number;
    notes: string;
  }) => {
    if (editingMeal) {
      await updateMeal.mutateAsync({ mealId: editingMeal.id, data });
    } else {
      await createMeal.mutateAsync(data);
    }
    setMealFormOpen(false);
    setEditingMeal(null);
  };

  const handleDelete = async () => {
    if (!deletingMeal) return;
    await deleteMeal.mutateAsync(deletingMeal.id);
    setDeletingMeal(null);
  };

  if (planLoading) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-lg text-muted-foreground">{t('diet_plan_detail.loading')}</div>
      </div>
    );
  }

  if (!plan) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-lg text-destructive">{t('diet_plan_detail.not_found')}</div>
      </div>
    );
  }

  const isSubmitting = createMeal.isPending || updateMeal.isPending;

  return (
    <div className="p-8 lg:p-10 animate-fade-in-up">
      <Link to="/diet-planner/diet-plans">
        <Button variant="ghost" size="sm" className="mb-4 -ml-2">
          <ArrowLeft className="mr-2 h-4 w-4" />
          {t('diet_plan_detail.back')}
        </Button>
      </Link>

      <div className="mb-8">
        <h1 className="text-4xl font-bold tracking-tight mb-3">{plan.name}</h1>
        <div className="flex items-center gap-4 text-muted-foreground">
          <div className="flex items-center gap-2">
            <CalendarIcon className="h-4 w-4" />
            <span>
              {new Date(plan.startDate).toLocaleDateString()} -{' '}
              {new Date(plan.endDate).toLocaleDateString()}
            </span>
          </div>
          <Badge>
            {plan.totalDays} {t('diet_plan_detail.days')}
          </Badge>
          <Badge variant="secondary">
            {plan.totalMeals} {t('diet_plan_detail.meals')}
          </Badge>
        </div>
      </div>

      {/* Week Navigator */}
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold">
            {t('diet_plan_detail.week_of', {
              date: weekDays[0].toLocaleDateString('en-US', { month: 'long', day: 'numeric' }),
            })}
          </h2>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            size="icon"
            onClick={() => {
              const d = new Date(selectedWeekStart);
              d.setDate(d.getDate() - 7);
              setSelectedWeekStart(d);
            }}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="icon"
            onClick={() => {
              const d = new Date(selectedWeekStart);
              d.setDate(d.getDate() + 7);
              setSelectedWeekStart(d);
            }}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {mealsLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="text-lg text-muted-foreground">{t('common.loading')}</div>
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-7 stagger-children">
          {weekDays.map((date) => {
            const dateStr = formatLocalDate(date);
            const dayMeals = mealsByDay[dateStr] || {};
            const isToday = dateStr === formatLocalDate(new Date());

            return (
              <Card
                key={dateStr}
                className={cn(
                  'min-h-[400px] transition-all duration-200',
                  isToday && 'border-primary/50 shadow-md shadow-primary/10'
                )}
              >
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm">
                    <div className="flex flex-col gap-1">
                      <span className="text-xs text-muted-foreground uppercase tracking-wider">
                        {date.toLocaleDateString('en-US', { weekday: 'short' })}
                      </span>
                      <span className={cn('text-xl font-bold', isToday && 'text-primary')}>
                        {date.getDate()}
                      </span>
                    </div>
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  {['breakfast', 'lunch', 'dinner', 'snack'].map((mealType) => (
                    <div key={mealType}>
                      <div className="flex items-center justify-between mb-1.5">
                        <h4 className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">
                          {t(`diet_plan_detail.meal_types.${mealType}`)}
                        </h4>
                        <button
                          type="button"
                          onClick={() => openCreateForm(dateStr, mealType)}
                          className="h-4 w-4 rounded text-muted-foreground/50 hover:text-primary transition-colors"
                          title={t('meal_form.add_title')}
                        >
                          <Plus className="h-3 w-3" />
                        </button>
                      </div>
                      {dayMeals[mealType]?.map((meal) => (
                        <div
                          key={meal.id}
                          className="group rounded-lg border bg-muted/30 p-2 text-xs mb-1.5 hover:bg-muted/50 transition-colors"
                        >
                          <div className="flex items-start justify-between gap-1">
                            <Link
                              to={`/diet-planner/recipes/${meal.recipeId}`}
                              className="font-medium hover:underline flex-1 min-w-0 truncate"
                            >
                              {meal.recipeName}
                            </Link>
                            <div className="flex gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity shrink-0">
                              <button
                                type="button"
                                onClick={() => openEditForm(meal)}
                                className="p-0.5 rounded text-muted-foreground hover:text-primary transition-colors"
                              >
                                <Pencil className="h-3 w-3" />
                              </button>
                              <button
                                type="button"
                                onClick={() => setDeletingMeal(meal)}
                                className="p-0.5 rounded text-muted-foreground hover:text-destructive transition-colors"
                              >
                                <Trash2 className="h-3 w-3" />
                              </button>
                            </div>
                          </div>
                          <p className="text-muted-foreground mt-0.5">
                            {t('recipes.servings', { count: Number(meal.servings) || 1 })}
                          </p>
                          {meal.notes && (
                            <p className="text-muted-foreground/70 mt-0.5 truncate">{meal.notes}</p>
                          )}
                        </div>
                      )) || (
                        <div className="rounded-lg border border-dashed p-2 text-xs text-muted-foreground/60 text-center">
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
      )}

      <MealForm
        key={`${editingMeal?.id ?? 'new'}-${String(mealFormOpen)}`}
        open={mealFormOpen}
        onClose={() => {
          setMealFormOpen(false);
          setEditingMeal(null);
        }}
        onSubmit={handleFormSubmit}
        initialDate={editingMeal ? undefined : mealFormDate}
        initialMealType={editingMeal ? undefined : mealFormType}
        initialValues={
          editingMeal
            ? {
                date: editingMeal.date,
                mealType: editingMeal.mealType,
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

      {/* Delete confirmation dialog */}
      <Dialog open={!!deletingMeal} onOpenChange={(v) => !v && setDeletingMeal(null)}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>{t('meal_form.delete_title')}</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            {t('meal_form.delete_description', { name: deletingMeal?.recipeName ?? '' })}
          </p>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setDeletingMeal(null)}
              disabled={deleteMeal.isPending}
            >
              {t('common.cancel')}
            </Button>
            <Button variant="destructive" onClick={handleDelete} disabled={deleteMeal.isPending}>
              {deleteMeal.isPending ? t('common.deleting') : t('common.delete')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
