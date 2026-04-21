import { useGoals } from '@modules/diet-planner/api/hooks/useGoals';
import {
  useMeals,
  useNutritionSummary,
  type DailyNutrition,
} from '@modules/diet-planner/api/hooks/useMeals';
import { useProducts } from '@modules/diet-planner/api/hooks/useProducts';
import { useProfile } from '@modules/diet-planner/api/hooks/useProfile';
import { useRecipes } from '@modules/diet-planner/api/hooks/useRecipes';
import { WeightPredictionCard } from '@modules/diet-planner/components/WeightPredictionCard';
import { GoalsForm } from '@modules/diet-planner/components/settings';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import {
  Package,
  BookOpen,
  CalendarDays,
  Loader2,
  ArrowRight,
  Target,
  Settings,
} from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

function calcPercent(actual: number | undefined, target: number | null | undefined): number {
  if (!target || !actual) return 0;
  return Math.min(Math.round((actual / target) * 100), 100);
}

function getTodayRange() {
  const today = new Date();
  const y = today.getFullYear();
  const m = String(today.getMonth() + 1).padStart(2, '0');
  const d = String(today.getDate()).padStart(2, '0');
  const dateStr = `${String(y)}-${m}-${d}`;
  return { from: dateStr, to: dateStr };
}

export default function Dashboard() {
  const { t } = useTranslation();
  const [goalsSheetOpen, setGoalsSheetOpen] = useState(false);
  const { data: productsData, isLoading: productsLoading } = useProducts({ pageSize: 1 });
  const { data: recipesData, isLoading: recipesLoading } = useRecipes({ pageSize: 1 });
  const todayRange = getTodayRange();
  const { data: todayMeals, isLoading: mealsLoading } = useMeals(todayRange);
  const { data: goalsData } = useGoals();
  const { data: nutritionData } = useNutritionSummary(todayRange);
  const { data: profile } = useProfile();
  const todayNutrition: DailyNutrition | undefined = nutritionData?.[0];

  const hasGoals =
    goalsData &&
    (goalsData.dailyCalorieTarget !== null ||
      goalsData.proteinGrams !== null ||
      goalsData.carbsGrams !== null ||
      goalsData.fatGrams !== null ||
      goalsData.fiberGrams !== null);

  const goalItems = [
    {
      label: t('dashboard.goal_calories'),
      value: goalsData?.dailyCalorieTarget ?? '\u2014',
      unit: ' kcal',
      color: 'bg-orange-500',
      percent: calcPercent(todayNutrition?.calories, goalsData?.dailyCalorieTarget),
    },
    {
      label: t('dashboard.goal_protein'),
      value: goalsData?.proteinGrams ?? '\u2014',
      unit: 'g',
      color: 'bg-blue-500',
      percent: calcPercent(todayNutrition?.protein, goalsData?.proteinGrams),
    },
    {
      label: t('dashboard.goal_carbs'),
      value: goalsData?.carbsGrams ?? '\u2014',
      unit: 'g',
      color: 'bg-emerald-500',
      percent: calcPercent(todayNutrition?.carbs, goalsData?.carbsGrams),
    },
    {
      label: t('dashboard.goal_fat'),
      value: goalsData?.fatGrams ?? '\u2014',
      unit: 'g',
      color: 'bg-amber-500',
      percent: calcPercent(todayNutrition?.fat, goalsData?.fatGrams),
    },
    {
      label: t('dashboard.goal_fiber'),
      value: goalsData?.fiberGrams ?? '\u2014',
      unit: 'g',
      color: 'bg-purple-500',
      percent: calcPercent(todayNutrition?.fiber, goalsData?.fiberGrams),
    },
  ];

  const productCount = productsData?.totalCount ?? 0;
  const recipeCount = recipesData?.totalCount ?? 0;
  const todayMealCount = todayMeals?.length ?? 0;

  const statCards = [
    {
      to: '/diet-planner/products',
      testId: 'product-card',
      icon: Package,
      title: t('common.products'),
      desc: t('dashboard.products_desc'),
      count: productCount,
      loading: productsLoading,
      countTestId: 'product-count',
      label: t('dashboard.total_products'),
      color: 'from-violet-500/10 to-purple-500/10 dark:from-violet-500/20 dark:to-purple-500/20',
      iconColor: 'text-violet-600 dark:text-violet-400',
    },
    {
      to: '/diet-planner/recipes',
      testId: 'recipe-card',
      icon: BookOpen,
      title: t('common.recipes'),
      desc: t('dashboard.recipes_desc'),
      count: recipeCount,
      loading: recipesLoading,
      countTestId: 'recipe-count',
      label: t('dashboard.total_recipes'),
      color: 'from-blue-500/10 to-cyan-500/10 dark:from-blue-500/20 dark:to-cyan-500/20',
      iconColor: 'text-blue-600 dark:text-blue-400',
    },
    {
      to: '/diet-planner/calendar',
      testId: 'calendar-card',
      icon: CalendarDays,
      title: t('common.calendar'),
      desc: t('dashboard.calendar_desc'),
      count: todayMealCount,
      loading: mealsLoading,
      countTestId: 'calendar-count',
      label: t('dashboard.meals_today'),
      color: 'from-emerald-500/10 to-teal-500/10 dark:from-emerald-500/20 dark:to-teal-500/20',
      iconColor: 'text-emerald-600 dark:text-emerald-400',
    },
  ];

  return (
    <div className="animate-fade-in-up p-8 lg:p-10">
      {/* Hero */}
      <div className="mb-10">
        <h1 className="mb-3 text-4xl font-bold tracking-tight">{t('dashboard.title')}</h1>
        <p className="text-muted-foreground text-lg">{t('dashboard.subtitle')}</p>
      </div>

      {/* Stat Cards */}
      <div className="stagger-children mb-10 grid gap-5 md:grid-cols-3">
        {statCards.map((card) => {
          const Icon = card.icon;
          return (
            <Link key={card.to} to={card.to} data-testid={card.testId} className="group">
              <Card className="hover:border-primary/30 relative cursor-pointer overflow-hidden border-transparent transition-all duration-300 hover:-translate-y-1 hover:shadow-lg">
                <div
                  className={cn(
                    'absolute inset-0 bg-gradient-to-br opacity-60 transition-opacity duration-300 group-hover:opacity-100',
                    card.color,
                  )}
                />
                <CardHeader className="relative">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <div className="bg-background/80 rounded-xl p-2.5 shadow-sm">
                        <Icon className={cn('h-5 w-5', card.iconColor)} />
                      </div>
                      <CardTitle className="text-lg">{card.title}</CardTitle>
                    </div>
                    <ArrowRight className="text-muted-foreground h-4 w-4 -translate-x-2 opacity-0 transition-all duration-300 group-hover:translate-x-0 group-hover:opacity-100" />
                  </div>
                  <CardDescription>{card.desc}</CardDescription>
                </CardHeader>
                <CardContent className="relative">
                  {card.loading ? (
                    <Loader2 className="text-muted-foreground h-6 w-6 animate-spin" />
                  ) : (
                    <>
                      <p
                        className="text-3xl font-bold tracking-tight"
                        data-testid={card.countTestId}
                      >
                        {card.count}
                      </p>
                      <p className="text-muted-foreground mt-1 text-sm">{card.label}</p>
                    </>
                  )}
                </CardContent>
              </Card>
            </Link>
          );
        })}
      </div>

      {/* Goal Progress / CTA */}
      {goalsData && hasGoals ? (
        <Card className="animate-fade-in-up mb-10" style={{ animationDelay: '100ms' }}>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="rounded-xl bg-orange-500/10 p-2.5">
                  <Target className="h-5 w-5 text-orange-600 dark:text-orange-400" />
                </div>
                <div>
                  <CardTitle className="text-lg">{t('dashboard.goals_title')}</CardTitle>
                  <CardDescription>{t('dashboard.goals_subtitle')}</CardDescription>
                </div>
              </div>
              <button
                type="button"
                onClick={() => {
                  setGoalsSheetOpen(true);
                }}
                className="text-muted-foreground hover:text-primary rounded-lg p-1.5 transition-colors"
                aria-label={t('dashboard.goals_edit')}
              >
                <Settings className="h-4 w-4" />
              </button>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
              {goalItems.map((item) => (
                <div key={item.label} className="space-y-2">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">{item.label}</span>
                    <span className="font-medium">
                      {item.value}
                      {item.unit}
                    </span>
                  </div>
                  <div className="bg-muted h-2 overflow-hidden rounded-full">
                    <div
                      className={cn('h-full rounded-full', item.color)}
                      style={{ width: `${String(item.percent)}%` }}
                    />
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      ) : (
        <Card
          className="animate-fade-in-up mb-10 border-dashed"
          style={{ animationDelay: '100ms' }}
        >
          <CardContent className="flex flex-col items-center gap-4 py-10 text-center">
            <div className="rounded-xl bg-orange-500/10 p-3">
              <Target className="h-7 w-7 text-orange-600 dark:text-orange-400" />
            </div>
            <div>
              <h3 className="text-lg font-semibold">{t('dashboard.goals_cta_title')}</h3>
              <p className="text-muted-foreground mt-1 text-sm">
                {t('dashboard.goals_cta_description')}
              </p>
            </div>
            <button
              type="button"
              onClick={() => {
                setGoalsSheetOpen(true);
              }}
              className="bg-primary text-primary-foreground hover:bg-primary/90 inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors"
            >
              {t('dashboard.goals_cta_button')}
              <ArrowRight className="h-4 w-4" />
            </button>
          </CardContent>
        </Card>
      )}

      {/* Weight Prediction */}
      <div className="animate-fade-in-up mb-10" style={{ animationDelay: '150ms' }}>
        <WeightPredictionCard hasProfile={!!profile} goalCalories={goalsData?.dailyCalorieTarget} />
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
    </div>
  );
}
