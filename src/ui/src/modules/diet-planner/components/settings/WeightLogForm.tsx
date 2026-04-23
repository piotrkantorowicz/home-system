import { zodResolver } from '@hookform/resolvers/zod';
import { useLogWeightEntry } from '@modules/diet-planner/api/hooks/useWeightEntries';
import { Button, DatePicker, Input, Label } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Loader2, Plus } from 'lucide-react';
import { Controller, useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const todayIso = () => new Date().toISOString().slice(0, 10);

const schema = z.object({
  date: z
    .string()
    .min(1, 'Date is required')
    .refine((d) => d <= todayIso(), 'Date cannot be in the future'),
  weightKg: z.coerce
    .number({ message: 'Weight is required' })
    .positive('Weight must be positive')
    .max(999, 'Weight is too large'),
});

type FormInput = z.input<typeof schema>;
type FormData = z.output<typeof schema>;

export interface WeightLogFormProps {
  onSuccess?: () => void;
  defaultDate?: string;
  className?: string;
}

export function WeightLogForm({ onSuccess, defaultDate, className }: WeightLogFormProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const mutation = useLogWeightEntry();

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors },
  } = useForm<FormInput, unknown, FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      date: defaultDate ?? todayIso(),
      weightKg: '' as unknown as number,
    },
  });

  const onSubmit = async (data: FormData) => {
    try {
      await mutation.mutateAsync({ date: data.date, weightKg: data.weightKg });
      toast.success(t('weightHistory.log_success', 'Weight logged'));
      reset({ date: defaultDate ?? todayIso(), weightKg: '' as unknown as number });
      onSuccess?.();
    } catch {
      toast.error(t('weightHistory.log_error', 'Failed to log weight'));
    }
  };

  return (
    <form
      onSubmit={(e) => {
        void handleSubmit(onSubmit)(e);
      }}
      className={className}
    >
      <div className="grid gap-4 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
        <div>
          <Label htmlFor="weight-date">{t('weightHistory.form.date', 'Date')}</Label>
          <Controller
            name="date"
            control={control}
            render={({ field }) => (
              <DatePicker
                testId="weight-date-picker"
                value={field.value}
                onChange={field.onChange}
                placeholder={t('weightHistory.form.date_placeholder', 'Pick a date')}
                className="mt-1"
              />
            )}
          />
          {errors.date && <p className="text-destructive mt-1 text-sm">{errors.date.message}</p>}
        </div>
        <div>
          <Label htmlFor="weight-kg">{t('weightHistory.form.weight_kg', 'Weight (kg)')}</Label>
          <Input
            id="weight-kg"
            type="number"
            step="0.1"
            min="0.1"
            max="999"
            placeholder="e.g., 75.5"
            className="mt-1"
            {...register('weightKg')}
          />
          {errors.weightKg && (
            <p className="text-destructive mt-1 text-sm">{errors.weightKg.message}</p>
          )}
        </div>
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              {t('common.saving', 'Saving…')}
            </>
          ) : (
            <>
              <Plus className="mr-2 h-4 w-4" />
              {t('weightHistory.form.submit', 'Log weight')}
            </>
          )}
        </Button>
      </div>
    </form>
  );
}
