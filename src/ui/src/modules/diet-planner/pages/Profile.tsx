import { zodResolver } from '@hookform/resolvers/zod';
import {
  useProfile,
  useCreateProfile,
  useUpdateProfile,
} from '@modules/diet-planner/api/hooks/useProfile';
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
import { User, Loader2, Save } from 'lucide-react';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const profileSchema = z.object({
  dateOfBirth: z.string().nullable().optional(),
  gender: z.enum(['Male', 'Female', 'Other']).nullable().optional(),
  heightCm: z.coerce.number().min(1).max(300).nullable().optional(),
  currentWeightKg: z.coerce.number().min(1).max(600).nullable().optional(),
  targetWeightKg: z.coerce.number().min(1).max(600).nullable().optional(),
  activityLevel: z
    .enum(['Sedentary', 'LightlyActive', 'ModeratelyActive', 'VeryActive', 'ExtraActive'])
    .nullable()
    .optional(),
});

type ProfileFormInput = z.input<typeof profileSchema>;
type ProfileFormData = z.output<typeof profileSchema>;

export default function Profile() {
  const { t } = useTranslation();
  const { data: profile, isLoading } = useProfile();
  const createMutation = useCreateProfile();
  const updateMutation = useUpdateProfile();
  const saveMutation = profile ? updateMutation : createMutation;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<ProfileFormInput, unknown, ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      dateOfBirth: null,
      gender: null,
      heightCm: null,
      currentWeightKg: null,
      targetWeightKg: null,
      activityLevel: null,
    },
  });

  useEffect(() => {
    if (profile) {
      reset({
        dateOfBirth: profile.dateOfBirth ?? null,
        gender: (profile.gender as ProfileFormInput['gender']) ?? null,
        heightCm: profile.heightCm ?? null,
        currentWeightKg: profile.currentWeightKg ?? null,
        targetWeightKg: profile.targetWeightKg ?? null,
        activityLevel: (profile.activityLevel as ProfileFormInput['activityLevel']) ?? null,
      });
    }
  }, [profile, reset]);

  const onSubmit = async (data: ProfileFormData) => {
    await saveMutation.mutateAsync({
      dateOfBirth: data.dateOfBirth ?? null,
      gender: data.gender ?? null,
      heightCm: data.heightCm ?? null,
      currentWeightKg: data.currentWeightKg ?? null,
      targetWeightKg: data.targetWeightKg ?? null,
      activityLevel: data.activityLevel ?? null,
    });
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
          <div className="rounded-xl bg-blue-500/10 p-2.5">
            <User className="h-6 w-6 text-blue-600 dark:text-blue-400" />
          </div>
          <h1 className="text-3xl font-bold tracking-tight">{t('profile.title')}</h1>
        </div>
        <p className="text-muted-foreground">{t('profile.subtitle')}</p>
      </div>

      <form
        onSubmit={(e) => {
          void handleSubmit(onSubmit)(e);
        }}
        className="space-y-6"
      >
        {/* Personal Info */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('profile.personal_header')}</CardTitle>
            <CardDescription>{t('profile.personal_desc')}</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-6 sm:grid-cols-2">
              <div>
                <Label htmlFor="dateOfBirth">{t('profile.date_of_birth')}</Label>
                <Input id="dateOfBirth" type="date" {...register('dateOfBirth')} />
                {errors.dateOfBirth && (
                  <p className="text-destructive mt-1 text-sm">{errors.dateOfBirth.message}</p>
                )}
              </div>
              <div>
                <Label htmlFor="gender">{t('profile.gender')}</Label>
                <select
                  id="gender"
                  className="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring mt-1 flex h-10 w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
                  {...register('gender')}
                >
                  <option value="">—</option>
                  <option value="Male">{t('profile.gender_male')}</option>
                  <option value="Female">{t('profile.gender_female')}</option>
                  <option value="Other">{t('profile.gender_other')}</option>
                </select>
                {errors.gender && (
                  <p className="text-destructive mt-1 text-sm">{errors.gender.message}</p>
                )}
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Body Measurements */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('profile.measurements_header')}</CardTitle>
            <CardDescription>{t('profile.measurements_desc')}</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-6 sm:grid-cols-3">
              <div>
                <Label htmlFor="heightCm">{t('profile.height')}</Label>
                <Input
                  id="heightCm"
                  type="number"
                  step="0.1"
                  placeholder="e.g., 175"
                  {...register('heightCm')}
                />
                {errors.heightCm && (
                  <p className="text-destructive mt-1 text-sm">{errors.heightCm.message}</p>
                )}
              </div>
              <div>
                <Label htmlFor="currentWeightKg">{t('profile.current_weight')}</Label>
                <Input
                  id="currentWeightKg"
                  type="number"
                  step="0.1"
                  placeholder="e.g., 75"
                  {...register('currentWeightKg')}
                />
                {errors.currentWeightKg && (
                  <p className="text-destructive mt-1 text-sm">{errors.currentWeightKg.message}</p>
                )}
              </div>
              <div>
                <Label htmlFor="targetWeightKg">{t('profile.target_weight')}</Label>
                <Input
                  id="targetWeightKg"
                  type="number"
                  step="0.1"
                  placeholder="e.g., 70"
                  {...register('targetWeightKg')}
                />
                {errors.targetWeightKg && (
                  <p className="text-destructive mt-1 text-sm">{errors.targetWeightKg.message}</p>
                )}
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Activity Level */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('profile.activity_header')}</CardTitle>
            <CardDescription>{t('profile.activity_desc')}</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="max-w-xs">
              <Label htmlFor="activityLevel">{t('profile.activity_level')}</Label>
              <select
                id="activityLevel"
                className="border-input bg-background ring-offset-background placeholder:text-muted-foreground focus-visible:ring-ring mt-1 flex h-10 w-full rounded-md border px-3 py-2 text-sm focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
                {...register('activityLevel')}
              >
                <option value="">—</option>
                <option value="Sedentary">{t('profile.activity_sedentary')}</option>
                <option value="LightlyActive">{t('profile.activity_lightly')}</option>
                <option value="ModeratelyActive">{t('profile.activity_moderately')}</option>
                <option value="VeryActive">{t('profile.activity_very')}</option>
                <option value="ExtraActive">{t('profile.activity_extra')}</option>
              </select>
              {errors.activityLevel && (
                <p className="text-destructive mt-1 text-sm">{errors.activityLevel.message}</p>
              )}
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
                {t('profile.save_btn')}
              </>
            )}
          </Button>
        </div>

        {saveMutation.isSuccess && (
          <p className="text-right text-sm text-emerald-600 dark:text-emerald-400">
            {t('profile.save_success')}
          </p>
        )}
      </form>
    </div>
  );
}
