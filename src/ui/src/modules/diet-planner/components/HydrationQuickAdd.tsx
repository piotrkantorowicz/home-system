import {
  useHydrationConfig,
  useWaterIntake,
  useLogWaterIntake,
  useDeleteWaterIntake,
} from '@modules/diet-planner/api/hooks/useHydration';
import { Card, CardContent } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Droplets, Minus, Plus, Loader2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';

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
  const toast = useToast();
  const today = formatDate(new Date());

  const { data: config } = useHydrationConfig();
  const { data: intake } = useWaterIntake(today);
  const logIntake = useLogWaterIntake();
  const deleteIntake = useDeleteWaterIntake();

  const glassSizeMl = config?.glassSizeMl ?? DEFAULT_GLASS_SIZE_ML;
  const dailyTargetMl = config?.dailyWaterTargetMl ?? DEFAULT_DAILY_TARGET_ML;
  const totalMl = intake?.totalMl ?? 0;
  const entries = intake?.entries ?? [];

  const currentGlasses = Math.floor(totalMl / glassSizeMl);
  const targetGlasses = Math.floor(dailyTargetMl / glassSizeMl);

  const isPending = logIntake.isPending || deleteIntake.isPending;

  const handleAdd = async () => {
    try {
      await logIntake.mutateAsync({ date: today, amountMl: glassSizeMl });
      toast.success(t('hydration.log_success', { amount: glassSizeMl }));
    } catch {
      toast.error(t('hydration.log_error'));
    }
  };

  const handleRemove = async () => {
    const lastEntry = entries[entries.length - 1];
    if (!lastEntry) return;
    try {
      await deleteIntake.mutateAsync({ id: lastEntry.id, date: today });
      toast.success(t('hydration.delete_success'));
    } catch {
      toast.error(t('hydration.delete_error'));
    }
  };

  return (
    <Card className="animate-fade-in-up mt-6">
      <CardContent className="flex items-center justify-between py-4">
        <div className="flex items-center gap-3">
          <div className="rounded-xl bg-blue-500/10 p-2">
            <Droplets className="h-5 w-5 text-blue-600 dark:text-blue-400" />
          </div>
          <span className="text-sm font-medium">{t('hydration.quick_title')}</span>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => {
              void handleRemove();
            }}
            disabled={isPending || entries.length === 0}
            aria-label={t('hydration.delete_entry_aria')}
            className="hover:bg-accent flex h-8 w-8 items-center justify-center rounded-lg border transition-colors disabled:opacity-40"
          >
            {deleteIntake.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Minus className="h-4 w-4" />
            )}
          </button>

          <span className="min-w-[100px] text-center text-sm font-semibold tabular-nums">
            {t('hydration.glasses_count', { current: currentGlasses, target: targetGlasses })}
          </span>

          <button
            type="button"
            onClick={() => {
              void handleAdd();
            }}
            disabled={isPending}
            aria-label={t('hydration.add_btn')}
            className="bg-primary text-primary-foreground hover:bg-primary/90 flex h-8 w-8 items-center justify-center rounded-lg transition-colors disabled:opacity-40"
          >
            {logIntake.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Plus className="h-4 w-4" />
            )}
          </button>
        </div>
      </CardContent>
    </Card>
  );
}
