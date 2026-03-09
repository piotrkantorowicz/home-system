import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
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
import { Target, Loader2, Save } from 'lucide-react';
import { useGoals, useCreateGoals, useUpdateGoals } from '@modules/diet-planner/api/hooks/useGoals';

const goalSchema = z.object({
  dailyCalorieTarget: z.coerce.number().min(0).max(20000).nullable().optional(),
  proteinGrams: z.coerce.number().min(0).max(1000).nullable().optional(),
  carbsGrams: z.coerce.number().min(0).max(1000).nullable().optional(),
  fatGrams: z.coerce.number().min(0).max(1000).nullable().optional(),
  fiberGrams: z.coerce.number().min(0).max(200).nullable().optional(),
});

type GoalFormData = z.infer<typeof goalSchema>;

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

export default function Goals() {
  const { t } = useTranslation('diet-planner');
  const { data: goals, isLoading } = useGoals();
  const createMutation = useCreateGoals();
  const updateMutation = useUpdateGoals();

  const goalsExist = goals != null && goals.id !== EMPTY_GUID;
  const saveMutation = goalsExist ? updateMutation : createMutation;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<GoalFormData>({
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
    await saveMutation.mutateAsync({
      dailyCalorieTarget: data.dailyCalorieTarget ?? null,
      proteinGrams: data.proteinGrams ?? null,
      carbsGrams: data.carbsGrams ?? null,
      fatGrams: data.fatGrams ?? null,
      fiberGrams: data.fiberGrams ?? null,
    });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="p-8 lg:p-10 max-w-4xl mx-auto animate-fade-in-up">
      {/* Hero */}
      <div className="mb-8">
        <div className="flex items-center gap-3 mb-3">
          <div className="rounded-xl bg-orange-500/10 p-2.5">
            <Target className="h-6 w-6 text-orange-600 dark:text-orange-400" />
          </div>
          <h1 className="text-3xl font-bold tracking-tight">{t('goals.title')}</h1>
        </div>
        <p className="text-muted-foreground">{t('goals.subtitle')}</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
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
                <p className="text-sm text-destructive mt-1">{errors.dailyCalorieTarget.message}</p>
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
                  <p className="text-sm text-destructive mt-1">{errors.proteinGrams.message}</p>
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
                  <p className="text-sm text-destructive mt-1">{errors.carbsGrams.message}</p>
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
                  <p className="text-sm text-destructive mt-1">{errors.fatGrams.message}</p>
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
                  <p className="text-sm text-destructive mt-1">{errors.fiberGrams.message}</p>
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
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
                {t('common.saving')}
              </>
            ) : (
              <>
                <Save className="h-4 w-4 mr-2" />
                {t('goals.save_btn')}
              </>
            )}
          </Button>
        </div>

        {saveMutation.isSuccess && (
          <p className="text-sm text-emerald-600 dark:text-emerald-400 text-right">
            {t('goals.save_success')}
          </p>
        )}
      </form>
    </div>
  );
}
