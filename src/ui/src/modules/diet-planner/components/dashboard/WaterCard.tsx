import {
  useHydrationConfig,
  useLogWaterIntake,
  useWaterIntake,
} from '@modules/diet-planner/api/hooks/useHydration';
import { Button, Card } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { cn } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

const DEFAULT_TARGET_ML = 2500;
const DEFAULT_GLASS_ML = 250;
const MAX_GLASSES = 12;

function today(): string {
  const d = new Date();
  return `${String(d.getFullYear())}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(
    d.getDate(),
  ).padStart(2, '0')}`;
}

export function WaterCard() {
  const { t } = useTranslation();
  const toast = useToast();
  const date = today();

  const { data: config } = useHydrationConfig();
  const { data: intake } = useWaterIntake(date);
  const logIntake = useLogWaterIntake();

  const glassMl = config?.glassSizeMl ?? DEFAULT_GLASS_ML;
  const targetMl = config?.dailyWaterTargetMl ?? DEFAULT_TARGET_ML;
  const totalMl = intake?.totalMl ?? 0;

  const targetGlasses = Math.min(MAX_GLASSES, Math.max(1, Math.round(targetMl / glassMl)));
  const filled = Math.floor(totalMl / glassMl);
  const partial = (totalMl % glassMl) / glassMl;
  const percent = targetMl > 0 ? Math.round((totalMl / targetMl) * 100) : 0;

  const add = async (amountMl: number) => {
    try {
      await logIntake.mutateAsync({ date, amountMl });
      toast.success(t('dashboard.water_logged', { amount: amountMl }));
    } catch {
      toast.error(t('dashboard.water_error'));
    }
  };

  return (
    <Card className="flex flex-col gap-4 p-[22px]">
      <div className="flex items-center justify-between">
        <div>
          <div className="text-[15px] font-bold">{t('dashboard.water_title')}</div>
          <div className="text-muted-foreground tnum text-[12.5px]">
            {(totalMl / 1000).toFixed(1)} / {(targetMl / 1000).toFixed(1)} L
          </div>
        </div>
        <div className="text-[24px] font-bold text-[hsl(var(--color-water))]">{percent}%</div>
      </div>

      <div className="flex gap-1.5">
        {Array.from({ length: targetGlasses }, (_, i) => {
          const state = i < filled ? 'full' : i === filled && partial > 0 ? 'partial' : 'empty';
          return (
            <div
              key={i}
              className={cn(
                'h-11 flex-1 rounded-[11px]',
                state === 'empty' && 'bg-muted border-border-strong border border-dashed',
              )}
              style={
                state === 'full'
                  ? { background: 'hsl(var(--color-water))' }
                  : state === 'partial'
                    ? { background: 'hsl(var(--color-water) / 0.55)' }
                    : undefined
              }
            />
          );
        })}
      </div>

      <div className="flex flex-wrap gap-2">
        <Button
          size="xs"
          variant="secondary"
          onClick={() => {
            void add(250);
          }}
          disabled={logIntake.isPending}
        >
          + 250 ml
        </Button>
        <Button
          size="xs"
          variant="secondary"
          onClick={() => {
            void add(500);
          }}
          disabled={logIntake.isPending}
        >
          + 500 ml
        </Button>
        <Button size="xs" variant="outline" asChild>
          <Link to="/diet-planner/hydration">{t('dashboard.water_custom')}</Link>
        </Button>
      </div>
    </Card>
  );
}
