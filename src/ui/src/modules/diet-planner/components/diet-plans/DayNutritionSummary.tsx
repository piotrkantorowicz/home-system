import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, X, AlertTriangle, TrendingUp, Minus } from 'lucide-react';
import { cn } from '@shared/lib/utils';
import type {
  DailyNutrition,
  DailyNutritionResponse,
} from '@modules/diet-planner/api/hooks/useDietPlans';

interface DayNutritionSummaryProps {
  nutrition: DailyNutrition;
  goals: DailyNutritionResponse['goals'];
}

const STATUS_CONFIG = {
  met: {
    color: 'bg-emerald-500',
    icon: Check,
    textColor: 'text-emerald-600 dark:text-emerald-400',
  },
  partial: {
    color: 'bg-amber-500',
    icon: AlertTriangle,
    textColor: 'text-amber-600 dark:text-amber-400',
  },
  missed: { color: 'bg-red-500', icon: X, textColor: 'text-red-600 dark:text-red-400' },
  exceeded: {
    color: 'bg-blue-500',
    icon: TrendingUp,
    textColor: 'text-blue-600 dark:text-blue-400',
  },
  no_goal: {
    color: 'bg-muted-foreground/30',
    icon: Minus,
    textColor: 'text-muted-foreground',
  },
} as const;

type StatusKey = keyof typeof STATUS_CONFIG;

function getStatusConfig(status: string) {
  return STATUS_CONFIG[status as StatusKey] ?? STATUS_CONFIG.no_goal;
}

function getOverallStatus(nutrition: DailyNutrition): StatusKey {
  const statuses = [
    nutrition.caloriesStatus,
    nutrition.proteinStatus,
    nutrition.carbsStatus,
    nutrition.fatStatus,
    nutrition.fiberStatus,
  ].filter((s) => s !== 'no_goal');

  if (statuses.length === 0) return 'no_goal';
  if (statuses.every((s) => s === 'met')) return 'met';
  if (statuses.some((s) => s === 'missed')) return 'missed';
  if (statuses.some((s) => s === 'partial')) return 'partial';
  return 'exceeded';
}

export function DayNutritionSummary({ nutrition, goals }: DayNutritionSummaryProps) {
  const { t } = useTranslation();
  const [showDetails, setShowDetails] = useState(false);

  if (nutrition.mealCount === 0) return null;

  const overall = getOverallStatus(nutrition);
  const OverallIcon = getStatusConfig(overall).icon;

  const macros = [
    {
      key: 'calories',
      label: t('nutrition.calories'),
      actual: nutrition.totalCalories,
      goal: goals?.dailyCalorieTarget,
      unit: 'kcal',
      status: nutrition.caloriesStatus,
    },
    {
      key: 'protein',
      label: t('nutrition.protein'),
      actual: nutrition.totalProtein,
      goal: goals?.proteinGrams,
      unit: 'g',
      status: nutrition.proteinStatus,
    },
    {
      key: 'carbs',
      label: t('nutrition.carbs'),
      actual: nutrition.totalCarbs,
      goal: goals?.carbsGrams,
      unit: 'g',
      status: nutrition.carbsStatus,
    },
    {
      key: 'fat',
      label: t('nutrition.fat'),
      actual: nutrition.totalFat,
      goal: goals?.fatGrams,
      unit: 'g',
      status: nutrition.fatStatus,
    },
    {
      key: 'fiber',
      label: t('nutrition.fiber'),
      actual: nutrition.totalFiber,
      goal: goals?.fiberGrams,
      unit: 'g',
      status: nutrition.fiberStatus,
    },
  ];

  return (
    <div
      className="relative mt-3 pt-3 border-t"
      onMouseEnter={() => setShowDetails(true)}
      onMouseLeave={() => setShowDetails(false)}
    >
      {/* Compact indicators row */}
      <div className="flex items-center gap-1.5">
        <OverallIcon className={cn('h-3 w-3', getStatusConfig(overall).textColor)} />
        <div className="flex gap-1 flex-1">
          {macros.map((macro) => (
            <div
              key={macro.key}
              className={cn(
                'h-1.5 flex-1 rounded-full transition-all',
                getStatusConfig(macro.status).color
              )}
            />
          ))}
        </div>
        <span className="text-[9px] text-muted-foreground font-medium">
          {Math.round(nutrition.totalCalories)}
        </span>
      </div>

      {/* Hover details popover */}
      {showDetails && (
        <div className="absolute bottom-full left-0 right-0 mb-2 z-50">
          <div className="bg-popover border rounded-lg shadow-lg p-3 text-xs">
            <h4 className="font-semibold mb-2">{t('nutrition.title')}</h4>
            <div className="space-y-1.5">
              {macros.map((macro) => {
                const config = getStatusConfig(macro.status);
                const StatusIcon = config.icon;
                const percentage = macro.goal
                  ? Math.round((macro.actual / macro.goal) * 100)
                  : null;

                return (
                  <div key={macro.key} className="flex items-center gap-2">
                    <StatusIcon className={cn('h-3 w-3 shrink-0', config.textColor)} />
                    <span className="text-muted-foreground w-14 truncate">{macro.label}</span>
                    <div className="flex-1 h-1.5 bg-muted rounded-full overflow-hidden">
                      <div
                        className={cn('h-full rounded-full transition-all', config.color)}
                        style={{ width: `${Math.min(percentage ?? 0, 100)}%` }}
                      />
                    </div>
                    <span className="font-medium tabular-nums w-20 text-right">
                      {Math.round(macro.actual)}
                      {macro.goal != null ? `/${macro.goal}` : ''} {macro.unit}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
