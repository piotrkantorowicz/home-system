import { zodResolver } from '@hookform/resolvers/zod';
import {
  useHydrationConfig,
  useUpdateHydrationConfig,
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
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Loader2, Save } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const DEFAULT_DAILY_TARGET_ML = 2500;
const DEFAULT_GLASS_SIZE_ML = 250;

const hydrationConfigSchema = z.object({
  dailyWaterTargetMl: z.coerce.number().min(100).max(10000),
  glassSizeMl: z.coerce.number().min(10).max(2000),
});

type HydrationConfigFormInput = z.input<typeof hydrationConfigSchema>;
type HydrationConfigFormData = z.output<typeof hydrationConfigSchema>;

export interface HydrationConfigFormProps {
  onSuccess?: () => void;
}

export function HydrationConfigForm({ onSuccess }: HydrationConfigFormProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const { data: config, isLoading } = useHydrationConfig();
  const updateConfigMutation = useUpdateHydrationConfig();

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
    },
    ...(config && {
      values: {
        dailyWaterTargetMl: config.dailyWaterTargetMl,
        glassSizeMl: config.glassSizeMl,
      },
    }),
  });

  const onSubmit = async (data: HydrationConfigFormData) => {
    try {
      await updateConfigMutation.mutateAsync({
        dailyWaterTargetMl: data.dailyWaterTargetMl,
        glassSizeMl: data.glassSizeMl,
        trackWaterIntake: config?.trackWaterIntake ?? true,
      });
      reset(data);
      toast.success(t('hydration.settings_saved'));
      onSuccess?.();
    } catch {
      toast.error(t('hydration.settings_save_error'));
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-10">
        <Loader2 className="text-muted-foreground h-6 w-6 animate-spin" />
      </div>
    );
  }

  return (
    <form
      onSubmit={(e) => {
        void handleSubmit(onSubmit)(e);
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
                <p className="text-destructive mt-1 text-sm">{errors.dailyWaterTargetMl.message}</p>
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
  );
}
