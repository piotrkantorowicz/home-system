import { zodResolver } from '@hookform/resolvers/zod';
import {
  useMealSchedule,
  useUpdateMealSchedule,
} from '@modules/diet-planner/api/hooks/useMealSchedule';
import { Card, CardContent, Button, Input, Label } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Loader2, Save, Plus, Trash2 } from 'lucide-react';
import { useEffect } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const MAX_SLOTS = 8;
const MIN_SLOTS = 1;

const DEFAULT_SLOTS = [{ name: '', defaultTime: '' }];

const mealSlotSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  defaultTime: z.string().min(1, 'Time is required'),
});

const mealScheduleSchema = z.object({
  slots: z.array(mealSlotSchema).min(MIN_SLOTS).max(MAX_SLOTS),
});

type MealScheduleFormData = z.infer<typeof mealScheduleSchema>;

export interface MealScheduleFormProps {
  onSuccess?: () => void;
}

export function MealScheduleForm({ onSuccess }: MealScheduleFormProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const { data: schedule, isLoading } = useMealSchedule();
  const updateMutation = useUpdateMealSchedule();

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isDirty },
  } = useForm<MealScheduleFormData>({
    resolver: zodResolver(mealScheduleSchema),
    defaultValues: {
      slots: DEFAULT_SLOTS,
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'slots',
  });

  useEffect(() => {
    if (schedule && schedule.slots.length > 0) {
      reset({
        slots: schedule.slots
          .slice()
          .sort((a, b) => Number(a.sortOrder) - Number(b.sortOrder))
          .map((slot) => ({ name: slot.name, defaultTime: slot.defaultTime })),
      });
    }
  }, [schedule, reset]);

  const onSubmit = async (data: MealScheduleFormData) => {
    try {
      await updateMutation.mutateAsync({ slots: data.slots });
      toast.success(t('meal_schedule.save_success'));
      onSuccess?.();
    } catch {
      toast.error(t('meal_schedule.save_error'));
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
      className="space-y-6"
    >
      <Card>
        <CardContent className="space-y-4 pt-6">
          <div className="grid grid-cols-[1fr_1fr_auto] gap-3 text-sm font-medium">
            <span>{t('meal_schedule.slot_name')}</span>
            <span>{t('meal_schedule.slot_time')}</span>
            <span />
          </div>

          {fields.map((field, index) => {
            const idxStr = String(index);
            const nameError = errors.slots?.[index]?.name;
            const timeError = errors.slots?.[index]?.defaultTime;

            return (
              <div key={field.id} className="grid grid-cols-[1fr_1fr_auto] items-start gap-3">
                <div>
                  <Label htmlFor={`slot-${idxStr}-name`} className="sr-only">
                    {t('meal_schedule.slot_name')}
                  </Label>
                  <Input
                    id={`slot-${idxStr}-name`}
                    type="text"
                    placeholder={t('meal_schedule.slot_name')}
                    // eslint-disable-next-line @typescript-eslint/restrict-template-expressions -- RHF requires number index
                    {...register(`slots.${index}.name`)}
                    aria-invalid={!!nameError}
                  />
                  {nameError && (
                    <p role="alert" className="text-destructive mt-1 text-xs">
                      {nameError.message}
                    </p>
                  )}
                </div>

                <div>
                  <Label htmlFor={`slot-${idxStr}-time`} className="sr-only">
                    {t('meal_schedule.slot_time')}
                  </Label>
                  <Input
                    id={`slot-${idxStr}-time`}
                    type="time"
                    // eslint-disable-next-line @typescript-eslint/restrict-template-expressions -- RHF requires number index
                    {...register(`slots.${index}.defaultTime`)}
                    aria-invalid={!!timeError}
                  />
                  {timeError && (
                    <p role="alert" className="text-destructive mt-1 text-xs">
                      {timeError.message}
                    </p>
                  )}
                </div>

                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  disabled={fields.length <= MIN_SLOTS}
                  onClick={() => {
                    remove(index);
                  }}
                  aria-label={t('meal_schedule.remove_slot')}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            );
          })}

          <Button
            type="button"
            variant="secondary"
            size="sm"
            disabled={fields.length >= MAX_SLOTS}
            onClick={() => {
              append({ name: '', defaultTime: '12:00' });
            }}
            className="mt-2"
          >
            <Plus className="mr-2 h-4 w-4" />
            {t('meal_schedule.add_slot')}
          </Button>
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button type="submit" disabled={updateMutation.isPending || !isDirty}>
          {updateMutation.isPending ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              {t('common.saving')}
            </>
          ) : (
            <>
              <Save className="mr-2 h-4 w-4" />
              {t('meal_schedule.save_btn')}
            </>
          )}
        </Button>
      </div>
    </form>
  );
}
