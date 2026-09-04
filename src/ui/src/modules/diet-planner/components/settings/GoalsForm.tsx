import { zodResolver } from '@hookform/resolvers/zod';
import { useGoals, useCreateGoals, useUpdateGoals } from '@modules/diet-planner/api/hooks/useGoals';
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  Button,
  Input,
  Field,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Loader2, Save } from 'lucide-react';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const goalSchema = z.object({
  dailyCalorieTarget: z.coerce.number().min(0).max(20000).nullable().optional(),
  proteinGrams: z.coerce.number().min(0).max(1000).nullable().optional(),
  carbsGrams: z.coerce.number().min(0).max(1000).nullable().optional(),
  fatGrams: z.coerce.number().min(0).max(1000).nullable().optional(),
  fiberGrams: z.coerce.number().min(0).max(200).nullable().optional(),
});

type GoalFormInput = z.input<typeof goalSchema>;
type GoalFormData = z.output<typeof goalSchema>;

export interface GoalsFormProps {
  onSuccess?: () => void;
}

const MACRO_FIELDS = [
  { id: 'proteinGrams', labelKey: 'goals.protein_label', phKey: 'goals.protein_placeholder' },
  { id: 'carbsGrams', labelKey: 'goals.carbs_label', phKey: 'goals.carbs_placeholder' },
  { id: 'fatGrams', labelKey: 'goals.fat_label', phKey: 'goals.fat_placeholder' },
  { id: 'fiberGrams', labelKey: 'goals.fiber_label', phKey: 'goals.fiber_placeholder' },
] as const;

export function GoalsForm({ onSuccess }: GoalsFormProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const { data: goals, isLoading } = useGoals();
  const createMutation = useCreateGoals();
  const updateMutation = useUpdateGoals();

  const goalsExist = goals !== null && goals !== undefined;
  const saveMutation = goalsExist ? updateMutation : createMutation;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<GoalFormInput, unknown, GoalFormData>({
    resolver: zodResolver(goalSchema),
    defaultValues: {
      dailyCalorieTarget: null,
      proteinGrams: null,
      carbsGrams: null,
      fatGrams: null,
      fiberGrams: null,
    },
  });

  useEffect(() => {
    if (goals && goalsExist) {
      reset({
        dailyCalorieTarget: goals.dailyCalorieTarget,
        proteinGrams: goals.proteinGrams,
        carbsGrams: goals.carbsGrams,
        fatGrams: goals.fatGrams,
        fiberGrams: goals.fiberGrams,
      });
    }
  }, [goals, goalsExist, reset]);

  const onSubmit = async (data: GoalFormData) => {
    try {
      await saveMutation.mutateAsync({
        dailyCalorieTarget: data.dailyCalorieTarget ?? null,
        proteinGrams: data.proteinGrams ?? null,
        carbsGrams: data.carbsGrams ?? null,
        fatGrams: data.fatGrams ?? null,
        fiberGrams: data.fiberGrams ?? null,
      });
      toast.success(t('goals.save_success'));
      onSuccess?.();
    } catch {
      toast.error(t('goals.save_error'));
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
      className="flex flex-col gap-[18px]"
    >
      <Card>
        <CardHeader>
          <CardTitle className="text-[15px]">{t('goals.nutrition_header')}</CardTitle>
          <CardDescription>{t('goals.nutrition_desc')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-5">
          <Field
            id="dailyCalorieTarget"
            label={t('goals.calories_label')}
            error={errors.dailyCalorieTarget?.message}
            className="max-w-xs"
          >
            <Input
              id="dailyCalorieTarget"
              type="number"
              step="1"
              className="tnum"
              placeholder={t('goals.calories_placeholder')}
              aria-invalid={!!errors.dailyCalorieTarget}
              {...register('dailyCalorieTarget')}
            />
          </Field>

          <div className="grid gap-4 sm:grid-cols-2">
            {MACRO_FIELDS.map((f) => (
              <Field key={f.id} id={f.id} label={t(f.labelKey)} error={errors[f.id]?.message}>
                <Input
                  id={f.id}
                  type="number"
                  step="0.1"
                  className="tnum"
                  placeholder={t(f.phKey)}
                  aria-invalid={!!errors[f.id]}
                  {...register(f.id)}
                />
              </Field>
            ))}
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end">
        <Button type="submit" size="xl" disabled={saveMutation.isPending || !isDirty}>
          {saveMutation.isPending ? (
            <>
              <Loader2 className="h-4 w-4 animate-spin" />
              {t('common.saving')}
            </>
          ) : (
            <>
              <Save className="h-4 w-4" />
              {t('goals.save_btn')}
            </>
          )}
        </Button>
      </div>
    </form>
  );
}
