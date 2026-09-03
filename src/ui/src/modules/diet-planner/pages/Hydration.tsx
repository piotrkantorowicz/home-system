import {
  useHydrationConfig,
  useWaterIntake,
  useLogWaterIntake,
  useDeleteWaterIntake,
} from '@modules/diet-planner/api/hooks/useHydration';
import { HydrationConfigForm } from '@modules/diet-planner/components/settings';
import {
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
import { useToast } from '@shared/context/ToastContext';
import { cn } from '@shared/lib/utils';
import { Droplet, Settings, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

const DEFAULT_TARGET_ML = 2500;
const DEFAULT_GLASS_ML = 250;
const MAX_GLASSES = 12;

function today(): string {
  const d = new Date();
  return `${String(d.getFullYear())}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(
    d.getDate(),
  ).padStart(2, '0')}`;
}

function formatTime(ts: string): string {
  return new Date(ts).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

export default function Hydration() {
  const { t } = useTranslation();
  const toast = useToast();
  const date = today();

  const { data: config, isLoading: configLoading } = useHydrationConfig();
  const { data: intake, isLoading: intakeLoading } = useWaterIntake(date);
  const logIntake = useLogWaterIntake();
  const deleteIntake = useDeleteWaterIntake();

  const [customAmount, setCustomAmount] = useState('');
  const [customNote, setCustomNote] = useState('');
  const [pending, setPending] = useState<number | null>(null);
  const [configOpen, setConfigOpen] = useState(false);

  const glassMl = config?.glassSizeMl ?? DEFAULT_GLASS_ML;
  const targetMl = config?.dailyWaterTargetMl ?? DEFAULT_TARGET_ML;
  const totalMl = intake?.totalMl ?? 0;
  const entries = intake?.entries ?? [];

  const percent = targetMl > 0 ? Math.min(100, Math.round((totalMl / targetMl) * 100)) : 0;
  const toGoMl = Math.max(0, targetMl - totalMl);
  const targetGlasses = Math.min(MAX_GLASSES, Math.max(1, Math.round(targetMl / glassMl)));
  const filled = Math.floor(totalMl / glassMl);
  const partial = (totalMl % glassMl) / glassMl;

  const add = async (amountMl: number, note?: string) => {
    setPending(amountMl);
    try {
      await logIntake.mutateAsync({ date, amountMl, ...(note ? { note } : {}) });
      toast.success(t('hydration.log_success', { amount: amountMl }));
    } catch {
      toast.error(t('hydration.log_error'));
    } finally {
      setPending(null);
    }
  };

  const addCustom = async () => {
    const amount = parseInt(customAmount, 10);
    if (Number.isNaN(amount) || amount <= 0) return;
    await add(amount, customNote.trim() || undefined);
    setCustomAmount('');
    setCustomNote('');
  };

  const remove = async (id: string) => {
    try {
      await deleteIntake.mutateAsync({ id, date });
      toast.success(t('hydration.delete_success'));
    } catch {
      toast.error(t('hydration.delete_error'));
    }
  };

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
        {/* Glass visual */}
        <div
          className="bg-secondary relative h-[176px] w-[132px] flex-none overflow-hidden border-2 border-[hsl(var(--color-water))]"
          style={{ borderRadius: '18px 18px 26px 26px' }}
        >
          <div
            className="absolute inset-x-0 bottom-0 transition-[height] duration-500"
            style={{
              height: `${String(percent)}%`,
              background:
                'linear-gradient(180deg, color-mix(in oklab, hsl(var(--color-water)) 75%, transparent), hsl(var(--color-water)))',
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

          <div className="flex gap-1.5">
            {Array.from({ length: targetGlasses }, (_, i) => {
              const state = i < filled ? 'full' : i === filled && partial > 0 ? 'partial' : 'empty';
              return (
                <div
                  key={i}
                  className={cn(
                    'h-[54px] flex-1 rounded-[12px]',
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
              size="xl"
              onClick={() => {
                void add(glassMl);
              }}
              disabled={pending !== null}
              style={{ background: 'hsl(var(--color-water))' }}
            >
              <Droplet className="size-4" />
              {t('hydration.add_glass', { amount: glassMl })}
            </Button>
            <Button
              size="xl"
              variant="secondary"
              onClick={() => {
                void add(500);
              }}
              disabled={pending !== null}
            >
              + 500 ml
            </Button>
            <Button
              size="xl"
              variant="secondary"
              onClick={() => {
                void add(750);
              }}
              disabled={pending !== null}
            >
              + 750 ml
            </Button>
          </div>

          <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
            <label className="text-text-2 flex-1 text-[12px] font-semibold">
              {t('hydration.custom_amount_label')}
              <input
                type="number"
                min="1"
                value={customAmount}
                onChange={(e) => {
                  setCustomAmount(e.target.value);
                }}
                placeholder={t('hydration.custom_amount_placeholder')}
                className="border-border bg-secondary mt-1 h-[42px] w-full rounded-[13px] border px-3 text-[13px] outline-none focus-visible:ring-2 focus-visible:ring-[hsl(var(--color-ring))]"
              />
            </label>
            <label className="text-text-2 flex-1 text-[12px] font-semibold">
              {t('hydration.custom_note_label')}
              <input
                type="text"
                value={customNote}
                onChange={(e) => {
                  setCustomNote(e.target.value);
                }}
                placeholder={t('hydration.custom_note_placeholder')}
                className="border-border bg-secondary mt-1 h-[42px] w-full rounded-[13px] border px-3 text-[13px] outline-none focus-visible:ring-2 focus-visible:ring-[hsl(var(--color-ring))]"
              />
            </label>
            <Button
              size="xl"
              variant="outline"
              onClick={() => {
                void addCustom();
              }}
              disabled={pending !== null || !customAmount}
            >
              {t('hydration.add_btn')}
            </Button>
          </div>
        </div>
      </Card>

      <Card className="flex flex-col gap-3 p-[22px]">
        <div className="text-[15px] font-bold">{t('hydration.entries_header')}</div>
        {entries.length === 0 ? (
          <EmptyState
            icon={Droplet}
            title={t('hydration.no_entries')}
            description={t('hydration.no_entries_desc')}
          />
        ) : (
          <div className="flex flex-col">
            {entries.map((entry) => (
              <div
                key={entry.id}
                className="border-border flex items-center gap-3 border-t py-2.5 first:border-t-0"
              >
                <span className="text-muted-foreground tnum w-11 flex-none text-[12px]">
                  {formatTime(entry.timestamp)}
                </span>
                <span className="min-w-0 flex-1 truncate text-[13.5px] font-semibold">
                  {entry.note ?? t('hydration.entry_water')}
                </span>
                <span className="tnum text-[13px] font-bold text-[hsl(var(--color-water))]">
                  {entry.amountMl} ml
                </span>
                <button
                  type="button"
                  onClick={() => {
                    void remove(entry.id);
                  }}
                  disabled={deleteIntake.isPending}
                  aria-label={t('hydration.delete_entry_aria')}
                  className="text-muted-foreground hover:text-destructive grid size-8 place-items-center rounded-[10px]"
                >
                  <Trash2 className="size-4" />
                </button>
              </div>
            ))}
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
