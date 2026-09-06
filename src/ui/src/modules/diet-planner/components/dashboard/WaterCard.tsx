import {
  useDeleteWaterIntake,
  useHydrationConfig,
  useLogWaterIntake,
  useWaterIntake,
} from '@modules/diet-planner/api/hooks/useHydration';
import {
  DEFAULT_GLASS_ML,
  DEFAULT_TARGET_ML,
  GlassRow,
} from '@modules/diet-planner/components/GlassRow';
import { WaterCustomAmountPopover } from '@modules/diet-planner/components/WaterCustomAmountPopover';
import { Button, Card } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

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
  const deleteIntake = useDeleteWaterIntake();

  const [pendingKeys, setPendingKeys] = useState<Set<string>>(new Set());

  const glassMl = config?.glassSizeMl ?? DEFAULT_GLASS_ML;
  const targetMl = config?.dailyWaterTargetMl ?? DEFAULT_TARGET_ML;
  const totalMl = intake?.totalMl ?? 0;
  const percent = targetMl > 0 ? Math.round((totalMl / targetMl) * 100) : 0;

  const entries = intake?.entries ?? [];
  const newestId = [...entries].sort(
    (a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime(),
  )[0]?.id;

  function clearPending(key: string) {
    setPendingKeys((prev) => {
      const next = new Set(prev);
      next.delete(key);
      return next;
    });
  }

  function add(amountMl: number, note?: string) {
    const key = `add-${String(amountMl)}-${String(Date.now())}`;
    setPendingKeys((prev) => new Set(prev).add(key));
    logIntake.mutate(
      { date, amountMl, ...(note ? { note } : {}) },
      {
        onSuccess: (newId) => {
          toast.success(t('dashboard.water_logged', { amount: amountMl }), {
            action: {
              label: t('hydration.undo'),
              onClick: () => {
                if (newId) deleteIntake.mutate({ id: newId, date });
              },
            },
          });
        },
        onError: () => {
          toast.error(t('dashboard.water_error'));
        },
        onSettled: () => {
          clearPending(key);
        },
      },
    );
  }

  function removeNewest() {
    if (!newestId) return;
    const key = `remove-${newestId}`;
    setPendingKeys((prev) => new Set(prev).add(key));
    deleteIntake.mutate(
      { id: newestId, date },
      {
        onSettled: () => {
          clearPending(key);
        },
      },
    );
  }

  const anyGlassPending = [...pendingKeys].some(
    (k) => k.startsWith('add-') || k.startsWith('remove-'),
  );

  return (
    <Card className="flex flex-col gap-4 p-[22px]">
      <div className="flex items-center justify-between">
        <div>
          <div className="text-[15px] font-bold">{t('dashboard.water_title')}</div>
          <div className="text-muted-foreground tnum text-[12.5px]">
            {(totalMl / 1000).toFixed(1)} / {(targetMl / 1000).toFixed(1)} L
          </div>
        </div>
        <div className="text-[24px] font-bold text-[var(--color-water)]">{percent}%</div>
      </div>

      <GlassRow
        totalMl={totalMl}
        targetMl={targetMl}
        glassMl={glassMl}
        size="sm"
        onAdd={() => {
          add(glassMl);
        }}
        onRemoveNewest={removeNewest}
        addDisabled={anyGlassPending}
        removeDisabled={anyGlassPending}
      />

      <div className="flex flex-wrap gap-2">
        <Button
          size="xs"
          variant="secondary"
          onClick={() => {
            add(250);
          }}
          disabled={pendingKeys.has('add-250')}
        >
          + 250 ml
        </Button>
        <Button
          size="xs"
          variant="secondary"
          onClick={() => {
            add(500);
          }}
          disabled={pendingKeys.has('add-500')}
        >
          + 500 ml
        </Button>
        <WaterCustomAmountPopover presets={[glassMl, 500, 750]} onAdd={add} triggerSize="sm" />
      </div>
    </Card>
  );
}
