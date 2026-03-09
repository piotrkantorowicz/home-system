import { useState, useMemo, useEffect } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, ChevronLeft, ChevronRight, Calendar as CalendarIcon } from 'lucide-react';
import { useDietPlan, useMeals } from '@modules/diet-planner/api/hooks/useDietPlans';
import { Button, Card, CardContent, CardHeader, CardTitle } from '@shared/components/ui';
import { Badge } from '@shared/components/ui/Badge';
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

export default function DietPlanDetail() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { data: plan, isLoading: planLoading } = useDietPlan(id!);
  const [selectedWeekStart, setSelectedWeekStart] = useState<Date>(new Date());

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

    return {
      start: formatLocalDate(start),
      end: formatLocalDate(end),
    };
  }, [selectedWeekStart]);

  const { data: meals, isLoading: mealsLoading } = useMeals(id!, weekRange.start, weekRange.end);

  const mealsByDay = useMemo(() => {
    if (!meals) return {};

    const grouped: Record<string, Record<string, (typeof meals)[0][]>> = {};

    meals.forEach((meal) => {
      const date = meal.date || '';
      const mealType = meal.mealType || 'other';

      if (!grouped[date]) {
        grouped[date] = {};
      }
      if (!grouped[date][mealType]) {
        grouped[date][mealType] = [];
      }
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

  const goToPreviousWeek = () => {
    const newDate = new Date(selectedWeekStart);
    newDate.setDate(newDate.getDate() - 7);
    setSelectedWeekStart(newDate);
  };

  const goToNextWeek = () => {
    const newDate = new Date(selectedWeekStart);
    newDate.setDate(newDate.getDate() + 7);
    setSelectedWeekStart(newDate);
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
          <Button variant="outline" size="icon" onClick={goToPreviousWeek}>
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <Button variant="outline" size="icon" onClick={goToNextWeek}>
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
                      <h4 className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wider mb-1.5">
                        {t(`diet_plan_detail.meal_types.${mealType}`)}
                      </h4>
                      {dayMeals[mealType]?.map((meal) => (
                        <div
                          key={meal.id}
                          className="rounded-lg border bg-muted/30 p-2 text-xs mb-1.5 hover:bg-muted/50 transition-colors"
                        >
                          <Link to={`/diet-planner/recipes/${meal.recipeId}`} className="hover:underline">
                            <p className="font-medium">{meal.recipeName}</p>
                          </Link>
                          <p className="text-muted-foreground mt-0.5">
                            {t('recipes.servings', { count: Number(meal.servings) || 1 })}
                          </p>
                        </div>
                      )) || (
                        <div className="rounded-lg border border-dashed p-2 text-xs text-muted-foreground/60 text-center">
                          {t('diet_plan_detail.no_meal')}
                        </div>
                      )}
                    </div>
                  ))}

                  {/* Daily totals commented out - MealEntryDto doesn't include nutrition data
                  {Object.keys(dayMeals).length > 0 && (
                    <div className="border-t pt-2 mt-2">
                      <p className="text-xs font-medium mb-1">
                        {t('diet_plan_detail.daily_totals')}
                      </p>
                      <div className="grid grid-cols-2 gap-1 text-xs">
                        <div>
                          <span className="text-muted-foreground">Cal:</span>{' '}
                          <span className="font-medium">{totals.calories.toFixed(0)}</span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">P:</span>{' '}
                          <span className="font-medium">{totals.protein.toFixed(0)}g</span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">C:</span>{' '}
                          <span className="font-medium">{totals.carbs.toFixed(0)}g</span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">F:</span>{' '}
                          <span className="font-medium">{totals.fat.toFixed(0)}g</span>
                        </div>
                      </div>
                    </div>
                  )}
                  */}
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
