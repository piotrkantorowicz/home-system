import { useGoals } from '@modules/diet-planner/api/hooks/useGoals';
import { useWeightPrediction } from '@modules/diet-planner/api/hooks/useWeightPrediction';
import { Button, Card, MetricTile } from '@shared/components/ui';
import { formatNumber, formatSigned } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';

export function EnergyModel() {
  const { t } = useTranslation();
  const { data: goals } = useGoals();
  const { data: prediction } = useWeightPrediction(goals?.dailyCalorieTarget ?? null);

  return (
    <Card className="p-22px flex flex-col gap-4">
      <div>
        <div className="text-15px font-bold">{t('profile.overview.energy_model')}</div>
        <div className="text-muted-foreground text-12-5px">
          {t('profile.overview.energy_model_sub')}
        </div>
      </div>
      {prediction ? (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          <MetricTile label="BMR" value={formatNumber(prediction.bmr)} hint="kcal" />
          <MetricTile label="TDEE" value={formatNumber(prediction.tdee)} hint="kcal" />
          <MetricTile
            label={t('profile.overview.daily_deficit')}
            value={formatSigned(-prediction.dailyDeficit)}
            accent="good"
            hint="kcal"
          />
          <MetricTile
            label={t('profile.overview.weekly_change')}
            value={`${formatSigned(prediction.weeklyWeightChange)} kg`}
          />
          <MetricTile
            label={t('profile.overview.current_bmi')}
            value={prediction.currentBmi.toFixed(1)}
          />
          <MetricTile
            label={t('profile.overview.target_bmi')}
            value={prediction.targetBmi !== null ? prediction.targetBmi.toFixed(1) : '—'}
          />
        </div>
      ) : (
        <div className="flex flex-col items-start gap-3">
          <p className="text-muted-foreground text-12-5px">{t('profile.energy_empty')}</p>
          <Button asChild variant="outline">
            <a href="#profile-details">{t('profile.add_details')}</a>
          </Button>
        </div>
      )}
    </Card>
  );
}
