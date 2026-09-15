import { useHydrationConfig, useWaterIntake } from '@modules/diet-planner/api/hooks/useHydration';
import { HydrationConfigForm } from '@modules/diet-planner/components/settings';
import {
  Banner,
  Button,
  Card,
  EmptyState,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  Skeleton,
} from '@shared/components/ui';
import { formatNumber } from '@shared/lib/utils';
import { Droplet, Settings, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useWaterActions } from '../api/hooks/useWaterActions';
import { DEFAULT_TARGET_ML, DEFAULT_GLASS_ML, GlassRow } from '../components/GlassRow';
import { WaterCustomAmountPopover } from '../components/WaterCustomAmountPopover';

import type { WaterIntakeEntryDto } from '@modules/diet-planner/api/hooks/useHydration';

function today(): string {
  const d = new Date();
  return `${String(d.getFullYear())}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(
    d.getDate(),
  ).padStart(2, '0')}`;
}

function formatTime(ts: string): string {
  return new Date(ts).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function mostUsedAmounts(entries: WaterIntakeEntryDto[], fallback: number[]): number[] {
  const counts = new Map<number, number>();
  for (const e of entries) counts.set(e.amountMl, (counts.get(e.amountMl) ?? 0) + 1);
  const result = [...counts.entries()].sort((a, b) => b[1] - a[1]).map(([amt]) => amt);
  for (const f of fallback) {
    if (result.length >= 3) break;
    if (!result.includes(f)) result.push(f);
  }
  return result.slice(0, 3);
}

export default function Hydration() {
  const { t } = useTranslation();
  const date = today();

  const configQuery = useHydrationConfig();
  const intakeQuery = useWaterIntake(date);
  const { data: config, isLoading: configLoading } = configQuery;
  const { data: intake, isLoading: intakeLoading } = intakeQuery;
  const { add, remove, pendingKeys } = useWaterActions(date);
  const [configOpen, setConfigOpen] = useState(false);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);

  const glassMl = config?.glassSizeMl ?? DEFAULT_GLASS_ML;
  const targetMl = config?.dailyWaterTargetMl ?? DEFAULT_TARGET_ML;
  const totalMl = intake?.totalMl ?? 0;
  const entries = intake?.entries ?? [];

  const percent = targetMl > 0 ? Math.min(100, Math.round((totalMl / targetMl) * 100)) : 0;
  const toGoMl = Math.max(0, targetMl - totalMl);
  const presets = mostUsedAmounts(entries, [glassMl, 500, 750]);

  function confirmRemove(id: string) {
    void remove(id).then((removed) => {
      if (removed) setConfirmDeleteId(null);
    });
  }

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

  if (configLoading || intakeLoading) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-6 md:px-8">
        <Skeleton className="h-[420px] w-full rounded-[22px]" />
      </div>
    );
  }

  return (
    <div className="animate-fade-in mx-auto flex max-w-4xl flex-col gap-6 px-4 py-6 md:px-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[26px] font-bold">{t('hydration.title')}</h1>
          <p className="text-muted-foreground mt-1 text-sm">{t('hydration.subtitle')}</p>
        </div>
        <Button
          size="xl"
          variant="outline"
          onClick={() => {
            setConfigOpen(true);
          }}
        >
          <Settings className="size-4" />
          {t('hydration.settings_header')}
        </Button>
      </div>

      <Card className="flex flex-col gap-6 p-6 sm:flex-row sm:items-center">
        <div
          role="meter"
          aria-label={t('hydration.level_aria')}
          aria-valuenow={percent}
          aria-valuemin={0}
          aria-valuemax={100}
          className="bg-secondary relative h-[176px] w-[132px] flex-none overflow-hidden border-2 border-[var(--color-water)]"
          style={{ borderRadius: '18px 18px 26px 26px' }}
        >
          <div
            className="absolute inset-x-0 bottom-0 transition-[height] duration-500"
            style={{
              height: `${String(percent)}%`,
              background:
                'linear-gradient(180deg, color-mix(in oklab, var(--color-water) 75%, transparent), var(--color-water))',
            }}
          />
          <div className="tnum absolute inset-0 grid place-items-center text-center text-[13px] font-bold">
            {(totalMl / 1000).toFixed(1)} L
            <br />
            <span className="text-text-2 text-[11px] font-medium">
              / {(targetMl / 1000).toFixed(1)} L
            </span>
          </div>
        </div>

        <div className="flex min-w-0 flex-1 flex-col gap-4">
          <div>
            <div className="numeral text-[22px] font-bold">
              {toGoMl > 0
                ? t('hydration.to_go', { amount: (toGoMl / 1000).toFixed(1) })
                : t('hydration.goal_reached')}
            </div>
            <div className="text-muted-foreground tnum text-[12.5px]">{percent}%</div>
          </div>

          <GlassRow
            totalMl={totalMl}
            targetMl={targetMl}
            glassMl={glassMl}
            size="lg"
            onAdd={() => {
              add(glassMl);
            }}
            informational
          />

          <div className="flex flex-wrap gap-2">
            <Button
              size="xl"
              onClick={() => {
                add(glassMl);
              }}
              disabled={pendingKeys.has(`add-${String(glassMl)}`)}
              style={{ background: 'var(--color-water)' }}
            >
              <Droplet className="size-4" />
              {t('hydration.add_glass', { amount: glassMl })}
            </Button>
            <Button
              size="xl"
              variant="secondary"
              onClick={() => {
                add(500);
              }}
              disabled={pendingKeys.has('add-500')}
            >
              + 500 ml
            </Button>
            <Button
              size="xl"
              variant="secondary"
              onClick={() => {
                add(750);
              }}
              disabled={pendingKeys.has('add-750')}
            >
              + 750 ml
            </Button>
            <WaterCustomAmountPopover presets={presets} onAdd={add} />
          </div>
          <p className="text-muted-foreground text-[11.5px] leading-relaxed">
            {t('hydration.quick_add_note')}
          </p>
        </div>
      </Card>

      <Card className="flex flex-col gap-3 p-[22px]">
        <div className="flex items-baseline justify-between gap-3">
          <div className="text-[15px] font-bold">{t('hydration.entries_header')}</div>
          {entries.length > 0 ? (
            <div className="tnum text-[13px] font-bold text-[var(--color-water)]">
              {formatNumber(totalMl)} ml
            </div>
          ) : null}
        </div>
        {entries.length === 0 ? (
          <EmptyState
            icon={Droplet}
            title={t('hydration.no_entries')}
            description={t('hydration.no_entries_desc')}
          />
        ) : (
          <div className="-mx-[22px] flex flex-col">
            {entries.map((entry) =>
              confirmDeleteId === entry.id ? (
                <div
                  key={entry.id}
                  className="border-border flex items-center gap-3 border-t px-[22px] py-2.5 first:border-t-0"
                  style={{ background: 'color-mix(in oklab, var(--color-fat) 7%, transparent)' }}
                >
                  <span className="text-muted-foreground tnum w-11 flex-none text-[12px]">
                    {formatTime(entry.timestamp)}
                  </span>
                  <span className="text-text-2 min-w-0 flex-1 truncate text-[12.5px]">
                    {t('hydration.remove_entry_confirm')}
                  </span>
                  <Button
                    size="xs"
                    variant="outline"
                    onClick={() => {
                      setConfirmDeleteId(null);
                    }}
                  >
                    {t('hydration.remove_entry_no')}
                  </Button>
                  <Button
                    size="xs"
                    variant="destructive"
                    disabled={pendingKeys.has(`remove-${entry.id}`)}
                    onClick={() => {
                      confirmRemove(entry.id);
                    }}
                  >
                    {t('hydration.remove_entry_yes')}
                  </Button>
                </div>
              ) : (
                <div
                  key={entry.id}
                  className="border-border flex items-center gap-3 border-t px-[22px] py-2.5 first:border-t-0"
                >
                  <span className="text-muted-foreground tnum w-11 flex-none text-[12px]">
                    {formatTime(entry.timestamp)}
                  </span>
                  <span className="min-w-0 flex-1 truncate text-[13.5px] font-semibold">
                    {entry.note ?? t('hydration.entry_water')}
                  </span>
                  <span className="tnum text-[13px] font-bold text-[var(--color-water)]">
                    {entry.amountMl} ml
                  </span>
                  <button
                    type="button"
                    onClick={() => {
                      setConfirmDeleteId(entry.id);
                    }}
                    aria-label={t('hydration.delete_entry_aria')}
                    className="text-muted-foreground hover:text-destructive grid size-8 place-items-center rounded-[10px]"
                  >
                    <Trash2 className="size-4" />
                  </button>
                </div>
              ),
            )}
          </div>
        )}
      </Card>

      <Sheet open={configOpen} onOpenChange={setConfigOpen}>
        <SheetContent side="right">
          <SheetHeader>
            <SheetTitle>{t('sheets.hydration.title')}</SheetTitle>
            <SheetDescription>{t('sheets.hydration.description')}</SheetDescription>
          </SheetHeader>
          <div className="mt-6 overflow-y-auto">
            <HydrationConfigForm
              onSuccess={() => {
                setConfigOpen(false);
              }}
            />
          </div>
        </SheetContent>
      </Sheet>
    </div>
  );
}
