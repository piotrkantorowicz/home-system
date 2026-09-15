import { zodResolver } from '@hookform/resolvers/zod';
import {
  useHydrationConfig,
  useUpdateHydrationConfig,
  useWaterIntake,
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
import { Droplets, Loader2, Save, Trash2 } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const DEFAULT_DAILY_TARGET_ML = 2500;
const DEFAULT_GLASS_SIZE_ML = 250;

const hydrationConfigSchema = z.object({
  dailyWaterTargetMl: z.coerce.number().min(100).max(10000),
  glassSizeMl: z.coerce.number().min(10).max(2000),
  trackWaterIntake: z.boolean(),
});

type HydrationConfigFormInput = z.input<typeof hydrationConfigSchema>;
type HydrationConfigFormData = z.output<typeof hydrationConfigSchema>;

function formatDate(date: Date): string {
  const year = String(date.getFullYear());
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function formatTime(timestamp: string): string {
  return new Date(timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

export interface HydrationConfigFormProps {
  onSuccess?: () => void;
}

export function HydrationConfigForm({ onSuccess }: HydrationConfigFormProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const today = formatDate(new Date());

  const { data: config, isLoading: configLoading } = useHydrationConfig();
  const { data: intake, isLoading: intakeLoading } = useWaterIntake(today);
  const updateConfigMutation = useUpdateHydrationConfig();
  const deleteIntakeMutation = useDeleteWaterIntake();

  const dailyTargetMl = config?.dailyWaterTargetMl ?? DEFAULT_DAILY_TARGET_ML;
  const totalMl = intake?.totalMl ?? 0;
  const entries = intake?.entries ?? [];
  const progressPercent = Math.min((totalMl / dailyTargetMl) * 100, 100);

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
    reset,
  } = useForm<HydrationConfigFormInput, unknown, HydrationConfigFormData>({
    resolver: zodResolver(hydrationConfigSchema),
    defaultValues: {
      dailyWaterTargetMl: DEFAULT_DAILY_TARGET_ML,
      glassSizeMl: DEFAULT_GLASS_SIZE_ML,
      trackWaterIntake: true,
    },
    ...(config && {
      values: {
        dailyWaterTargetMl: config.dailyWaterTargetMl,
        glassSizeMl: config.glassSizeMl,
        trackWaterIntake: config.trackWaterIntake,
      },
    }),
  });

  const handleDelete = async (id: string) => {
    try {
      await deleteIntakeMutation.mutateAsync({ id, date: today });
      toast.success(t('hydration.delete_success'));
    } catch {
      toast.error(t('hydration.delete_error'));
    }
  };

  const onSubmit = async (data: HydrationConfigFormData) => {
    try {
      await updateConfigMutation.mutateAsync(data);
    } catch {
      toast.error(t('hydration.settings_save_error'));
      return;
    }
    reset(data);
    toast.success(t('hydration.settings_saved'));
    onSuccess?.();
  };

  if (configLoading || intakeLoading) {
    return (
      <div className="flex items-center justify-center py-10">
        <Loader2 className="text-muted-foreground h-6 w-6 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Today's Intake */}
      <div className="space-y-4">
        <h3 className="text-base font-semibold">{t('hydration.today_section')}</h3>

        {/* Progress */}
        <Card>
          <CardContent className="pt-6">
            <div className="mb-2 flex items-center justify-between">
              <span className="text-muted-foreground text-sm">{t('hydration.progress_label')}</span>
              <span className="text-sm font-medium">
                {totalMl} / {dailyTargetMl} ml
              </span>
            </div>
            <div className="bg-muted h-3 w-full overflow-hidden rounded-full">
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

        {/* Entry list */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('hydration.entries_header')}</CardTitle>
          </CardHeader>
          <CardContent>
            {entries.length === 0 ? (
              <EmptyState
                icon={Droplets}
                title={t('hydration.no_entries')}
                description={t('hydration.no_entries_desc')}
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

      {/* Settings */}
      <div className="space-y-4">
        <h3 className="text-base font-semibold">{t('hydration.settings_section')}</h3>
        <form
          onSubmit={(e) => {
            void handleSubmit(onSubmit)(e);
          }}
          className="space-y-4"
        >
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t('hydration.settings_header')}</CardTitle>
              <CardDescription>{t('hydration.settings_desc')}</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <Label htmlFor="dailyWaterTargetMl">{t('hydration.daily_target_label')}</Label>
                  <Input
                    id="dailyWaterTargetMl"
                    type="number"
                    step="50"
                    aria-invalid={!!errors.dailyWaterTargetMl}
                    placeholder="2500"
                    {...register('dailyWaterTargetMl')}
                  />
                  {errors.dailyWaterTargetMl && (
                    <p className="text-destructive mt-1 text-[11.5px]">
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
                    aria-invalid={!!errors.glassSizeMl}
                    placeholder="250"
                    {...register('glassSizeMl')}
                  />
                  {errors.glassSizeMl && (
                    <p className="text-destructive mt-1 text-[11.5px]">
                      {errors.glassSizeMl.message}
                    </p>
                  )}
                </div>
                <div className="flex items-center gap-3 sm:col-span-2">
                  <Checkbox id="trackWaterIntake" {...register('trackWaterIntake')} />
                  <Label htmlFor="trackWaterIntake">{t('hydration.track_toggle_label')}</Label>
                </div>
              </div>
            </CardContent>
          </Card>

          <div className="flex justify-end">
            <Button type="submit" size="xl" disabled={updateConfigMutation.isPending || !isDirty}>
              {updateConfigMutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  {t('common.saving')}
                </>
              ) : (
                <>
                  <Save className="h-4 w-4" />
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
