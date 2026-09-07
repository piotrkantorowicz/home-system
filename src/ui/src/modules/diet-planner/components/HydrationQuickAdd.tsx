import { useHydrationConfig, useWaterIntake } from '@modules/diet-planner/api/hooks/useHydration';
import { Card, CardContent } from '@shared/components/ui';
import { Droplets, Plus, Loader2, Settings } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { useWaterActions } from '../api/hooks/useWaterActions';

const DEFAULT_DAILY_TARGET_ML = 2500;
const DEFAULT_GLASS_SIZE_ML = 250;

function formatDate(date: Date): string {
  const year = String(date.getFullYear());
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function HydrationQuickAdd() {
  const { t } = useTranslation();
  const today = formatDate(new Date());

  const { data: config } = useHydrationConfig();
  const { data: intake } = useWaterIntake(today);
  const { add, pendingKeys } = useWaterActions(today);

  const glassSizeMl = config?.glassSizeMl ?? DEFAULT_GLASS_SIZE_ML;
  const dailyTargetMl = config?.dailyWaterTargetMl ?? DEFAULT_DAILY_TARGET_ML;
  const totalMl = intake?.totalMl ?? 0;

  const currentGlasses = Math.floor(totalMl / glassSizeMl);
  const targetGlasses = Math.floor(dailyTargetMl / glassSizeMl);

  const isPending = pendingKeys.has(`add-${String(glassSizeMl)}`);
  if (config?.trackWaterIntake === false) return null;

  return (
    <Card className="animate-fade-in-up mt-6">
      <CardContent className="flex flex-wrap items-center justify-between gap-3 py-4">
        <div className="flex items-center gap-3">
          <div className="rounded-xl bg-blue-500/10 p-2">
            <Droplets className="h-5 w-5 text-blue-600 dark:text-blue-400" />
          </div>
          <span className="text-sm font-medium">{t('hydration.quick_title')}</span>
        </div>

        <div className="flex items-center gap-3">
          <span className="min-w-[100px] text-center text-sm font-semibold tabular-nums">
            {t('hydration.glasses_count', { current: currentGlasses, target: targetGlasses })}
          </span>

          <button
            type="button"
            onClick={() => {
              add(glassSizeMl);
            }}
            disabled={isPending}
            aria-label={t('hydration.add_btn')}
            className="bg-primary text-primary-foreground hover:bg-primary/90 flex h-11 w-11 items-center justify-center rounded-lg transition-colors disabled:opacity-40"
          >
            {isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Plus className="h-4 w-4" />
            )}
          </button>

          <Link
            to="/diet-planner/hydration"
            className="text-muted-foreground hover:text-foreground flex h-11 w-11 items-center justify-center rounded-lg transition-colors"
            aria-label={t('hydration.settings_header')}
          >
            <Settings className="h-4 w-4" />
          </Link>
        </div>
      </CardContent>
    </Card>
  );
}
