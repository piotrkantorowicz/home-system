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
  Label,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Target, Loader2, Save } from 'lucide-react';
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

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

export default function Goals() {
  const { t } = useTranslation();
  const toast = useToast();
  const { data: goals, isLoading } = useGoals();
  const createMutation = useCreateGoals();
  const updateMutation = useUpdateGoals();

  const goalsExist = goals !== null && goals !== undefined && goals.id !== EMPTY_GUID;
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
    } catch {
      toast.error(t('goals.save_error'));
    }
  };

  if (isLoading) {
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
          <div className="rounded-xl bg-orange-500/10 p-2.5">
            <Target className="h-6 w-6 text-orange-600 dark:text-orange-400" />
          </div>
          <h1 className="text-3xl font-bold tracking-tight">{t('goals.title')}</h1>
        </div>
        <p className="text-muted-foreground">{t('goals.subtitle')}</p>
      </div>

      <form
        onSubmit={(e) => {
          void handleSubmit(onSubmit)(e);
        }}
        className="space-y-6"
      >
        {/* Calories */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('goals.calories_header')}</CardTitle>
            <CardDescription>{t('goals.calories_desc')}</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="max-w-xs">
              <Label htmlFor="dailyCalorieTarget">{t('goals.calories_label')}</Label>
              <Input
                id="dailyCalorieTarget"
                type="number"
                step="1"
                placeholder={t('goals.calories_placeholder')}
                {...register('dailyCalorieTarget')}
              />
              {errors.dailyCalorieTarget && (
                <p className="text-destructive mt-1 text-sm">{errors.dailyCalorieTarget.message}</p>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Macros */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('goals.macros_header')}</CardTitle>
            <CardDescription>{t('goals.macros_desc')}</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-6 sm:grid-cols-2">
              <div>
                <Label htmlFor="proteinGrams">{t('goals.protein_label')}</Label>
                <Input
                  id="proteinGrams"
                  type="number"
                  step="0.1"
                  placeholder={t('goals.protein_placeholder')}
                  {...register('proteinGrams')}
                />
                {errors.proteinGrams && (
                  <p className="text-destructive mt-1 text-sm">{errors.proteinGrams.message}</p>
                )}
              </div>
              <div>
                <Label htmlFor="carbsGrams">{t('goals.carbs_label')}</Label>
                <Input
                  id="carbsGrams"
                  type="number"
                  step="0.1"
                  placeholder={t('goals.carbs_placeholder')}
                  {...register('carbsGrams')}
                />
                {errors.carbsGrams && (
                  <p className="text-destructive mt-1 text-sm">{errors.carbsGrams.message}</p>
                )}
              </div>
              <div>
                <Label htmlFor="fatGrams">{t('goals.fat_label')}</Label>
                <Input
                  id="fatGrams"
                  type="number"
                  step="0.1"
                  placeholder={t('goals.fat_placeholder')}
                  {...register('fatGrams')}
                />
                {errors.fatGrams && (
                  <p className="text-destructive mt-1 text-sm">{errors.fatGrams.message}</p>
                )}
              </div>
              <div>
                <Label htmlFor="fiberGrams">{t('goals.fiber_label')}</Label>
                <Input
                  id="fiberGrams"
                  type="number"
                  step="0.1"
                  placeholder={t('goals.fiber_placeholder')}
                  {...register('fiberGrams')}
                />
                {errors.fiberGrams && (
                  <p className="text-destructive mt-1 text-sm">{errors.fiberGrams.message}</p>
                )}
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Submit */}
        <div className="flex justify-end">
          <Button type="submit" disabled={saveMutation.isPending || !isDirty}>
            {saveMutation.isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                {t('common.saving')}
              </>
            ) : (
              <>
                <Save className="mr-2 h-4 w-4" />
                {t('goals.save_btn')}
              </>
            )}
          </Button>
        </div>
      </form>
    </div>
  );
}
