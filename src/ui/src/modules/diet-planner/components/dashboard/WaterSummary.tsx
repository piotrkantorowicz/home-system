import { useHydrationConfig, useWaterIntake } from '@modules/diet-planner/api/hooks/useHydration';
import { useWaterActions } from '@modules/diet-planner/api/hooks/useWaterActions';
import { DEFAULT_GLASS_ML, DEFAULT_TARGET_ML } from '@modules/diet-planner/components/GlassRow';
import { WaterCustomAmountPopover } from '@modules/diet-planner/components/WaterCustomAmountPopover';
import { Banner, Button, Skeleton } from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

const QUICK_ADD_ML = [250, 500];

/** The Water column of the Today summary: progress toward the target and one-tap logging. */
export function WaterSummary({ date }: { date: string }) {
  const { t } = useTranslation();
  const fmt = useFormat();
  const configQuery = useHydrationConfig();
  const intakeQuery = useWaterIntake(date);
  const { add, pendingKeys } = useWaterActions(date);
  const config = configQuery.data;
  const total = intakeQuery.data?.totalMl ?? 0;
  const target = config?.dailyWaterTargetMl ?? DEFAULT_TARGET_ML;
  const glass = config?.glassSizeMl ?? DEFAULT_GLASS_ML;

  if (configQuery.isError || intakeQuery.isError)
    return (
      <Banner
        variant="error"
        onRetry={() => {
          void configQuery.refetch();
          void intakeQuery.refetch();
        }}
        retryLabel={t('dashboard.retry')}
      >
        {t('dashboard.data_error')}
      </Banner>
    );
  if (configQuery.isPending || intakeQuery.isPending) return <Skeleton className="h-32 w-full" />;
  if (config?.trackWaterIntake === false)
    return (
      <div className="flex flex-col gap-2">
        <h3 className="text-label font-semibold">{t('dashboard.water_title')}</h3>
        <Link
          to="/diet-planner/hydration"
          className="text-primary inline-flex min-h-11 items-center text-sm font-semibold"
        >
          {t('dashboard.water_settings')}
        </Link>
      </div>
    );

  // "1.3 of 2.5 L": the first token is the figure that gets the large type.
  const [amount = '', ...rest] = fmt
    .waterProgress(total, target, t('dashboard.water_of'))
    .split(' ');

  return (
    <div className="flex min-w-0 flex-col gap-3">
      <h3 className="text-label font-semibold">{t('dashboard.water_title')}</h3>
      <p className="text-text-2 flex flex-wrap items-baseline gap-x-2 text-sm">
        <strong className="numeral text-foreground text-[32px] leading-none font-semibold">
          {amount}
        </strong>
        <span>{rest.join(' ')}</span>
      </p>
      <div
        role="progressbar"
        aria-label={t('dashboard.water_title')}
        aria-valuemin={0}
        aria-valuemax={target}
        aria-valuenow={Math.min(total, target)}
        className="bg-muted h-2 overflow-hidden rounded-full"
      >
        <div
          className="bg-water h-full rounded-full"
          style={{ width: `${String(target > 0 ? Math.min(100, (total / target) * 100) : 0)}%` }}
        />
      </div>
      <div className="flex flex-wrap gap-2">
        {QUICK_ADD_ML.map((ml) => (
          <Button
            key={ml}
            variant="outline"
            className="flex-1"
            disabled={pendingKeys.has(`add-${String(ml)}`)}
            onClick={() => {
              add(ml);
            }}
          >
            +{fmt.volume(ml)}
          </Button>
        ))}
        <WaterCustomAmountPopover presets={[glass, 500, 750]} onAdd={add} />
      </div>
    </div>
  );
}
