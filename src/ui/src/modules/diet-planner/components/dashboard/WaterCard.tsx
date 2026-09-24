import { useHydrationConfig, useWaterIntake } from '@modules/diet-planner/api/hooks/useHydration';
import { useWaterActions } from '@modules/diet-planner/api/hooks/useWaterActions';
import { DEFAULT_GLASS_ML, DEFAULT_TARGET_ML } from '@modules/diet-planner/components/GlassRow';
import { WaterCustomAmountPopover } from '@modules/diet-planner/components/WaterCustomAmountPopover';
import { Banner, Button, Card, Skeleton } from '@shared/components/ui';
import { formatNumber } from '@shared/lib/utils';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

export function WaterCard() {
  const { t, i18n } = useTranslation();
  const now = new Date();
  const date = `${String(now.getFullYear())}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
  const configQuery = useHydrationConfig();
  const intakeQuery = useWaterIntake(date);
  const { add, remove, pendingKeys } = useWaterActions(date);
  const [confirmId, setConfirmId] = useState<string | null>(null);
  const config = configQuery.data;
  const total = intakeQuery.data?.totalMl ?? 0;
  const target = config?.dailyWaterTargetMl ?? DEFAULT_TARGET_ML;
  const glass = config?.glassSizeMl ?? DEFAULT_GLASS_ML;
  const entries = [...(intakeQuery.data?.entries ?? [])].sort((a, b) =>
    b.timestamp.localeCompare(a.timestamp),
  );
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
  if (configQuery.isPending || intakeQuery.isPending) return <Skeleton className="h-60 w-full" />;
  if (config?.trackWaterIntake === false)
    return (
      <Card className="p-6">
        <Link to="/diet-planner/hydration">{t('dashboard.water_settings')}</Link>
      </Card>
    );
  return (
    <Card className="flex min-w-0 flex-col gap-4 p-6">
      <h2 className="text-lg font-semibold">{t('dashboard.water_title')}</h2>
      <p className="text-text-2 text-sm">
        <strong className="numeral text-foreground text-3xl">{formatNumber(total)}</strong> /{' '}
        {formatNumber(target)} ml
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
        {[250, 500].map((amount) => (
          <Button
            key={amount}
            variant="secondary"
            disabled={pendingKeys.has(`add-${String(amount)}`)}
            onClick={() => {
              add(amount);
            }}
          >
            + {amount} ml
          </Button>
        ))}
        <WaterCustomAmountPopover presets={[glass, 500, 750]} onAdd={add} />
      </div>
      <details className="border-t">
        <summary className="text-text-2 min-h-11 cursor-pointer py-3 text-sm">
          {t('dashboard.water_entries', { count: entries.length })}
        </summary>
        {entries.map((entry) => (
          <div
            key={entry.id}
            className="flex flex-wrap items-center justify-between gap-2 border-t py-3 text-sm"
          >
            <div>
              <span>
                {entry.amountMl} ml ·{' '}
                {new Date(entry.timestamp).toLocaleTimeString(i18n.language, {
                  hour: '2-digit',
                  minute: '2-digit',
                })}
              </span>
              {entry.note && <p className="text-text-2 break-words">{entry.note}</p>}
            </div>
            {confirmId === entry.id ? (
              <div className="flex flex-wrap items-center gap-2">
                <span>{t('hydration.remove_entry_confirm')}</span>
                <Button
                  variant="ghost"
                  onClick={() => {
                    setConfirmId(null);
                  }}
                >
                  {t('hydration.remove_entry_no')}
                </Button>
                <Button
                  variant="destructive"
                  disabled={pendingKeys.has(`remove-${entry.id}`)}
                  onClick={() => {
                    void remove(entry.id).then((removed) => {
                      if (removed) setConfirmId(null);
                    });
                  }}
                >
                  {t('hydration.remove_entry_yes')}
                </Button>
              </div>
            ) : (
              <Button
                variant="ghost"
                onClick={() => {
                  setConfirmId(entry.id);
                }}
                aria-label={t('dashboard.remove_water_entry', {
                  amount: entry.amountMl,
                  time: new Date(entry.timestamp).toLocaleTimeString(i18n.language),
                })}
              >
                {t('hydration.remove_entry_yes')}
              </Button>
            )}
          </div>
        ))}
      </details>
    </Card>
  );
}
