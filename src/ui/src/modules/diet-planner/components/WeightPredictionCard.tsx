import { useWeightPrediction } from '@modules/diet-planner/api/hooks/useWeightPrediction';
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  Input,
  Label,
} from '@shared/components/ui';
import { Activity, Loader2, TrendingDown, TrendingUp, Minus } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

function WeightTrendIcon({ weeklyChange }: { weeklyChange: number }) {
  if (weeklyChange < -0.01) {
    return <TrendingDown className="h-5 w-5 text-emerald-500" aria-hidden />;
  }
  if (weeklyChange > 0.01) {
    return <TrendingUp className="h-5 w-5 text-orange-500" aria-hidden />;
  }
  return <Minus className="h-5 w-5 text-muted-foreground" aria-hidden />;
}

function BmiCategory(bmi: number): string {
  if (bmi < 18.5) return 'underweight';
  if (bmi < 25) return 'normal';
  if (bmi < 30) return 'overweight';
  return 'obese';
}

const BMI_COLORS: Record<string, string> = {
  underweight: 'text-blue-500',
  normal: 'text-emerald-500',
  overweight: 'text-orange-500',
  obese: 'text-red-500',
};

const BMI_LABELS: Record<string, string> = {
  underweight: 'prediction.bmi_underweight',
  normal: 'prediction.bmi_normal',
  overweight: 'prediction.bmi_overweight',
  obese: 'prediction.bmi_obese',
};

export interface WeightPredictionCardProps {
  /** Whether the user's profile has enough data to calculate a prediction */
  hasProfile: boolean;
}

export function WeightPredictionCard({ hasProfile }: WeightPredictionCardProps) {
  const { t } = useTranslation();
  const [calorieInput, setCalorieInput] = useState<string>('');
  const [debouncedCalories, setDebouncedCalories] = useState<number | null>(null);

  // Simple debounce: only trigger query after the user stops typing
  const handleCalorieChange = (value: string) => {
    setCalorieInput(value);
    const parsed = parseFloat(value);
    setDebouncedCalories(!isNaN(parsed) && parsed > 0 ? parsed : null);
  };

  const { data: prediction, isLoading } = useWeightPrediction(debouncedCalories);

  const weeklyChange = Number(prediction?.weeklyWeightChange ?? 0);
  const weeklyChangeAbs = Math.abs(weeklyChange);
  const isLoss = weeklyChange < -0.01;
  const isGain = weeklyChange > 0.01;

  const currentBmiCategory = prediction ? BmiCategory(Number(prediction.currentBmi)) : null;
  const targetBmiCategory =
    prediction?.targetBmi !== undefined && prediction.targetBmi !== null
      ? BmiCategory(Number(prediction.targetBmi))
      : null;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Activity className="h-5 w-5 text-blue-600 dark:text-blue-400" aria-hidden />
          <CardTitle className="text-lg">{t('prediction.header')}</CardTitle>
        </div>
        <CardDescription>{t('prediction.desc')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Calorie target input */}
        <div className="max-w-xs">
          <Label htmlFor="dailyCalorieTarget">{t('prediction.calorie_target_label')}</Label>
          <Input
            id="dailyCalorieTarget"
            type="number"
            step="50"
            min="500"
            max="10000"
            placeholder={t('prediction.calorie_target_placeholder')}
            value={calorieInput}
            onChange={(e) => {
              handleCalorieChange(e.target.value);
            }}
            className="mt-1"
          />
          <p className="text-muted-foreground mt-1 text-xs">{t('prediction.calorie_target_help')}</p>
        </div>

        {!hasProfile && (
          <p className="text-muted-foreground text-sm">{t('prediction.no_profile')}</p>
        )}

        {hasProfile && !debouncedCalories && (
          <p className="text-muted-foreground text-sm">{t('prediction.enter_calories')}</p>
        )}

        {hasProfile && debouncedCalories !== null && isLoading && (
          <div className="flex items-center gap-2 text-sm">
            <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
            <span>{t('common.loading')}</span>
          </div>
        )}

        {hasProfile && debouncedCalories !== null && !isLoading && prediction === null && (
          <p className="text-muted-foreground text-sm">{t('prediction.incomplete_profile')}</p>
        )}

        {prediction && (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {/* BMR */}
            <div className="rounded-lg border p-3">
              <p className="text-muted-foreground text-xs font-medium uppercase tracking-wide">
                {t('prediction.bmr_label')}
              </p>
              <p className="mt-1 text-2xl font-bold">{Number(prediction.bmr).toFixed(0)}</p>
              <p className="text-muted-foreground text-xs">{t('prediction.kcal_day')}</p>
            </div>

            {/* TDEE */}
            <div className="rounded-lg border p-3">
              <p className="text-muted-foreground text-xs font-medium uppercase tracking-wide">
                {t('prediction.tdee_label')}
              </p>
              <p className="mt-1 text-2xl font-bold">{Number(prediction.tdee).toFixed(0)}</p>
              <p className="text-muted-foreground text-xs">{t('prediction.kcal_day')}</p>
            </div>

            {/* Daily deficit / surplus */}
            <div className="rounded-lg border p-3">
              <p className="text-muted-foreground text-xs font-medium uppercase tracking-wide">
                {Number(prediction.dailyDeficit) >= 0
                  ? t('prediction.daily_surplus_label')
                  : t('prediction.daily_deficit_label')}
              </p>
              <p
                className={`mt-1 text-2xl font-bold ${
                  Number(prediction.dailyDeficit) < 0 ? 'text-emerald-500' : 'text-orange-500'
                }`}
              >
                {Math.abs(Number(prediction.dailyDeficit)).toFixed(0)}
              </p>
              <p className="text-muted-foreground text-xs">{t('prediction.kcal_day')}</p>
            </div>

            {/* Weekly weight change */}
            <div className="rounded-lg border p-3">
              <p className="text-muted-foreground text-xs font-medium uppercase tracking-wide">
                {t('prediction.weekly_change_label')}
              </p>
              <div className="mt-1 flex items-center gap-1.5">
                <WeightTrendIcon weeklyChange={weeklyChange} />
                <p className="text-2xl font-bold">
                  {isLoss
                    ? `−${weeklyChangeAbs.toFixed(2)}`
                    : isGain
                      ? `+${weeklyChangeAbs.toFixed(2)}`
                      : '0.00'}
                </p>
              </div>
              <p className="text-muted-foreground text-xs">{t('prediction.kg_week')}</p>
            </div>

            {/* Current BMI */}
            <div className="rounded-lg border p-3">
              <p className="text-muted-foreground text-xs font-medium uppercase tracking-wide">
                {t('prediction.current_bmi_label')}
              </p>
              <p
                className={`mt-1 text-2xl font-bold ${currentBmiCategory !== null ? (BMI_COLORS[currentBmiCategory] ?? '') : ''}`}
              >
                {Number(prediction.currentBmi).toFixed(1)}
              </p>
              {currentBmiCategory !== null && (
                <p className="text-muted-foreground text-xs">
                  {t(BMI_LABELS[currentBmiCategory] ?? '')}
                </p>
              )}
            </div>

            {/* Target BMI */}
            {prediction.targetBmi !== null && (
              <div className="rounded-lg border p-3">
                <p className="text-muted-foreground text-xs font-medium uppercase tracking-wide">
                  {t('prediction.target_bmi_label')}
                </p>
                <p
                  className={`mt-1 text-2xl font-bold ${targetBmiCategory !== null ? (BMI_COLORS[targetBmiCategory] ?? '') : ''}`}
                >
                  {Number(prediction.targetBmi).toFixed(1)}
                </p>
                {targetBmiCategory !== null && (
                  <p className="text-muted-foreground text-xs">
                    {t(BMI_LABELS[targetBmiCategory] ?? '')}
                  </p>
                )}
              </div>
            )}

            {/* Estimated goal date */}
            {prediction.estimatedGoalDate !== null && (
              <div className="rounded-lg border p-3 sm:col-span-2 lg:col-span-3">
                <p className="text-muted-foreground text-xs font-medium uppercase tracking-wide">
                  {t('prediction.goal_date_label')}
                </p>
                <p className="mt-1 text-xl font-bold">
                  {new Date(prediction.estimatedGoalDate).toLocaleDateString(undefined, {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })}
                </p>
                <p className="text-muted-foreground text-xs">{t('prediction.goal_date_desc')}</p>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
