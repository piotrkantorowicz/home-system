import { zodResolver } from '@hookform/resolvers/zod';
import {
  useHydrationConfig,
  useUpdateHydrationConfig,
  useWaterIntake,
  useLogWaterIntake,
  useDeleteWaterIntake,
} from '@modules/diet-planner/api/hooks/useHydration';
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  Button,
  Input,
  Label,
  Checkbox,
  EmptyState,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Droplets, Loader2, Save, Trash2, Plus } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const DEFAULT_DAILY_TARGET_ML = 2500;
const DEFAULT_GLASS_SIZE_ML = 250;

const settingsSchema = z.object({
  dailyWaterTargetMl: z.coerce.number().min(100).max(10000),
  glassSizeMl: z.coerce.number().min(10).max(2000),
  trackWaterIntake: z.boolean(),
});

type SettingsFormInput = z.input<typeof settingsSchema>;
type SettingsFormData = z.output<typeof settingsSchema>;

function formatDate(date: Date): string {
  const year = String(date.getFullYear());
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function formatTime(timestamp: string): string {
  return new Date(timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

export default function Hydration() {
  const { t } = useTranslation();
  const toast = useToast();
  const today = formatDate(new Date());

  const { data: config, isLoading: configLoading } = useHydrationConfig();
  const { data: intake, isLoading: intakeLoading } = useWaterIntake(today);
  const updateConfigMutation = useUpdateHydrationConfig();
  const logIntakeMutation = useLogWaterIntake();
  const deleteIntakeMutation = useDeleteWaterIntake();

  const [customAmount, setCustomAmount] = useState<string>('');
  const [customNote, setCustomNote] = useState<string>('');

  const glassSizeMl = config?.glassSizeMl ?? DEFAULT_GLASS_SIZE_ML;
  const dailyTargetMl = config?.dailyWaterTargetMl ?? DEFAULT_DAILY_TARGET_ML;
  const totalMl = intake?.totalMl ?? 0;
  const entries = intake?.entries ?? [];

  const progressPercent = Math.min((totalMl / dailyTargetMl) * 100, 100);

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
    reset: resetSettings,
  } = useForm<SettingsFormInput, unknown, SettingsFormData>({
    resolver: zodResolver(settingsSchema),
    defaultValues: {
      dailyWaterTargetMl: config?.dailyWaterTargetMl ?? DEFAULT_DAILY_TARGET_ML,
      glassSizeMl: config?.glassSizeMl ?? DEFAULT_GLASS_SIZE_ML,
      trackWaterIntake: config?.trackWaterIntake ?? true,
    },
    ...(config && {
      values: {
        dailyWaterTargetMl: config.dailyWaterTargetMl,
        glassSizeMl: config.glassSizeMl,
        trackWaterIntake: config.trackWaterIntake,
      },
    }),
  });

  const handleQuickAdd = async (amountMl: number) => {
    try {
      await logIntakeMutation.mutateAsync({ date: today, amountMl });
      toast.success(t('hydration.log_success', { amount: amountMl }));
    } catch {
      toast.error(t('hydration.log_error'));
    }
  };

  const handleCustomAdd = async () => {
    const amount = parseInt(customAmount, 10);
    if (isNaN(amount) || amount <= 0) return;
    const trimmedNote = customNote.trim();
    try {
      await logIntakeMutation.mutateAsync({
        date: today,
        amountMl: amount,
        ...(trimmedNote && { note: trimmedNote }),
      });
      toast.success(t('hydration.log_success', { amount }));
      setCustomAmount('');
      setCustomNote('');
    } catch {
      toast.error(t('hydration.log_error'));
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await deleteIntakeMutation.mutateAsync({ id, date: today });
      toast.success(t('hydration.delete_success'));
    } catch {
      toast.error(t('hydration.delete_error'));
    }
  };

  const onSettingsSubmit = async (data: SettingsFormData) => {
    try {
      await updateConfigMutation.mutateAsync(data);
      resetSettings(data);
      toast.success(t('hydration.settings_saved'));
    } catch {
      toast.error(t('hydration.settings_save_error'));
    }
  };

  if (configLoading || intakeLoading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <Loader2 className="text-muted-foreground h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="animate-fade-in-up mx-auto max-w-4xl p-8 lg:p-10">
      {/* Hero */}
      <div className="mb-8">
        <div className="mb-3 flex items-center gap-3">
          <div className="rounded-xl bg-blue-500/10 p-2.5">
            <Droplets className="h-6 w-6 text-blue-600 dark:text-blue-400" />
          </div>
          <h1 className="text-3xl font-bold tracking-tight">{t('hydration.title')}</h1>
        </div>
        <p className="text-muted-foreground">{t('hydration.subtitle')}</p>
      </div>

      {/* Today's Water Intake */}
      <div className="mb-8 space-y-6">
        <h2 className="text-xl font-semibold">{t('hydration.today_section')}</h2>

        {/* Progress */}
        <Card>
          <CardContent className="pt-6">
            <div className="mb-2 flex items-center justify-between">
              <span className="text-muted-foreground text-sm">{t('hydration.progress_label')}</span>
              <span className="text-sm font-medium">
                {totalMl} / {dailyTargetMl} ml
              </span>
            </div>
            <div className="bg-muted h-4 w-full overflow-hidden rounded-full">
              <div
                className="h-full rounded-full bg-blue-500 transition-all duration-500"
                style={{ width: `${String(progressPercent)}%` }}
                role="progressbar"
                aria-valuenow={totalMl}
                aria-valuemin={0}
                aria-valuemax={dailyTargetMl}
                aria-label={t('hydration.progress_aria')}
              />
            </div>
            <p className="text-muted-foreground mt-2 text-center text-sm">
              {progressPercent >= 100
                ? t('hydration.goal_reached')
                : t('hydration.remaining', { amount: dailyTargetMl - totalMl })}
            </p>
          </CardContent>
        </Card>

        {/* Quick-add buttons */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('hydration.quick_add_header')}</CardTitle>
            <CardDescription>{t('hydration.quick_add_desc')}</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-3">
              <Button
                type="button"
                variant="secondary"
                onClick={() => {
                  void handleQuickAdd(glassSizeMl);
                }}
                disabled={logIntakeMutation.isPending}
              >
                <Droplets className="mr-2 h-4 w-4" />
                {t('hydration.add_glass', { amount: glassSizeMl })}
              </Button>
              <Button
                type="button"
                variant="secondary"
                onClick={() => {
                  void handleQuickAdd(500);
                }}
                disabled={logIntakeMutation.isPending}
              >
                +500 ml
              </Button>
              <Button
                type="button"
                variant="secondary"
                onClick={() => {
                  void handleQuickAdd(250);
                }}
                disabled={logIntakeMutation.isPending}
              >
                +250 ml
              </Button>
            </div>

            {/* Custom amount */}
            <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-end">
              <div className="flex-1">
                <Label htmlFor="customAmount">{t('hydration.custom_amount_label')}</Label>
                <Input
                  id="customAmount"
                  type="number"
                  min="1"
                  placeholder={t('hydration.custom_amount_placeholder')}
                  value={customAmount}
                  onChange={(e) => {
                    setCustomAmount(e.target.value);
                  }}
                />
              </div>
              <div className="flex-1">
                <Label htmlFor="customNote">{t('hydration.custom_note_label')}</Label>
                <Input
                  id="customNote"
                  type="text"
                  placeholder={t('hydration.custom_note_placeholder')}
                  value={customNote}
                  onChange={(e) => {
                    setCustomNote(e.target.value);
                  }}
                />
              </div>
              <Button
                type="button"
                onClick={() => {
                  void handleCustomAdd();
                }}
                disabled={logIntakeMutation.isPending || !customAmount}
              >
                <Plus className="mr-2 h-4 w-4" />
                {t('hydration.add_btn')}
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Entry list */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('hydration.entries_header')}</CardTitle>
          </CardHeader>
          <CardContent>
            {entries.length === 0 ? (
              <EmptyState
                icon={Droplets}
                title={t('hydration.no_entries')}
                description="Start tracking your water intake today"
              />
            ) : (
              <ul className="space-y-2">
                {entries.map((entry) => (
                  <li
                    key={entry.id}
                    className="bg-muted/40 flex items-center justify-between rounded-lg px-4 py-3"
                  >
                    <div className="flex items-center gap-3">
                      <Droplets className="h-4 w-4 text-blue-500" />
                      <div>
                        <span className="font-medium">{entry.amountMl} ml</span>
                        {entry.note && (
                          <span className="text-muted-foreground ml-2 text-sm">{entry.note}</span>
                        )}
                      </div>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className="text-muted-foreground text-sm">
                        {formatTime(entry.timestamp)}
                      </span>
                      <Button
                        type="button"
                        variant="ghost"
                        onClick={() => {
                          void handleDelete(entry.id);
                        }}
                        disabled={deleteIntakeMutation.isPending}
                        aria-label={t('hydration.delete_entry_aria')}
                      >
                        <Trash2 className="h-4 w-4 text-red-500" />
                      </Button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Hydration Settings */}
      <div className="space-y-4">
        <h2 className="text-xl font-semibold">{t('hydration.settings_section')}</h2>

        <form
          onSubmit={(e) => {
            void handleSubmit(onSettingsSubmit)(e);
          }}
          className="space-y-4"
        >
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">{t('hydration.settings_header')}</CardTitle>
              <CardDescription>{t('hydration.settings_desc')}</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid gap-6 sm:grid-cols-2">
                <div>
                  <Label htmlFor="dailyWaterTargetMl">{t('hydration.daily_target_label')}</Label>
                  <Input
                    id="dailyWaterTargetMl"
                    type="number"
                    step="50"
                    placeholder="2500"
                    {...register('dailyWaterTargetMl')}
                  />
                  {errors.dailyWaterTargetMl && (
                    <p className="text-destructive mt-1 text-sm">
                      {errors.dailyWaterTargetMl.message}
                    </p>
                  )}
                </div>
                <div>
                  <Label htmlFor="glassSizeMl">{t('hydration.glass_size_label')}</Label>
                  <Input
                    id="glassSizeMl"
                    type="number"
                    step="10"
                    placeholder="250"
                    {...register('glassSizeMl')}
                  />
                  {errors.glassSizeMl && (
                    <p className="text-destructive mt-1 text-sm">{errors.glassSizeMl.message}</p>
                  )}
                </div>
                <div className="flex items-center gap-3">
                  <Checkbox id="trackWaterIntake" {...register('trackWaterIntake')} />
                  <Label htmlFor="trackWaterIntake">{t('hydration.track_toggle_label')}</Label>
                </div>
              </div>
            </CardContent>
          </Card>

          <div className="flex justify-end">
            <Button type="submit" disabled={updateConfigMutation.isPending || !isDirty}>
              {updateConfigMutation.isPending ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  {t('common.saving')}
                </>
              ) : (
                <>
                  <Save className="mr-2 h-4 w-4" />
                  {t('hydration.save_settings_btn')}
                </>
              )}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
