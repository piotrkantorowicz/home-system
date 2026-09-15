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
  DatePicker,
  Select,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Loader2, Save } from 'lucide-react';
import { useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const bodyStatsSchema = z.object({
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

type BodyStatsFormInput = z.input<typeof bodyStatsSchema>;
type BodyStatsFormData = z.output<typeof bodyStatsSchema>;

export interface BodyStatsFormProps {
  onSuccess?: () => void;
}

export function BodyStatsForm({ onSuccess }: BodyStatsFormProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const { data: profile, isLoading } = useProfile();
  const createMutation = useCreateProfile();
  const updateMutation = useUpdateProfile();
  const saveMutation = profile ? updateMutation : createMutation;

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors, isDirty },
  } = useForm<BodyStatsFormInput, unknown, BodyStatsFormData>({
    resolver: zodResolver(bodyStatsSchema),
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
        gender: (profile.gender as BodyStatsFormInput['gender']) ?? null,
        heightCm: profile.heightCm ?? null,
        currentWeightKg: profile.currentWeightKg ?? null,
        targetWeightKg: profile.targetWeightKg ?? null,
        activityLevel: (profile.activityLevel as BodyStatsFormInput['activityLevel']) ?? null,
      });
    }
  }, [profile, reset]);

  const onSubmit = async (data: BodyStatsFormData) => {
    const request = {
      dateOfBirth: data.dateOfBirth ?? null,
      gender: data.gender ?? null,
      heightCm: data.heightCm ?? null,
      currentWeightKg: data.currentWeightKg ?? null,
      targetWeightKg: data.targetWeightKg ?? null,
      activityLevel: data.activityLevel ?? null,
    };
    try {
      await saveMutation.mutateAsync(request);
    } catch {
      toast.error(t('profile.save_error'));
      return;
    }
    toast.success(t('profile.save_success'));
    onSuccess?.();
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
      {/* Personal Info — dateOfBirth, gender, activityLevel */}
      <Card>
        <CardHeader>
          <CardTitle className="text-[15px]">{t('profile.personal_header')}</CardTitle>
          <CardDescription>{t('profile.personal_desc')}</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-3">
            <div>
              <Label>{t('profile.date_of_birth')}</Label>
              <Controller
                name="dateOfBirth"
                control={control}
                render={({ field }) => (
                  <DatePicker
                    testId="date-of-birth-picker"
                    value={field.value}
                    onChange={field.onChange}
                    placeholder={t('profile.date_of_birth_placeholder', 'Pick a date')}
                    className="mt-1"
                  />
                )}
              />
              {errors.dateOfBirth && (
                <p className="text-destructive mt-1 text-[11.5px]">{errors.dateOfBirth.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="gender">{t('profile.gender')}</Label>
              <Select
                id="gender"
                className="mt-1"
                aria-invalid={!!errors.gender}
                {...register('gender')}
              >
                <option value="">—</option>
                <option value="Male">{t('profile.gender_male')}</option>
                <option value="Female">{t('profile.gender_female')}</option>
                <option value="Other">{t('profile.gender_other')}</option>
              </Select>
              {errors.gender && (
                <p className="text-destructive mt-1 text-[11.5px]">{errors.gender.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="activityLevel">{t('profile.activity_level')}</Label>
              <Select
                id="activityLevel"
                className="mt-1"
                aria-invalid={!!errors.activityLevel}
                {...register('activityLevel')}
              >
                <option value="">—</option>
                <option value="Sedentary">{t('profile.activity_sedentary')}</option>
                <option value="LightlyActive">{t('profile.activity_lightly')}</option>
                <option value="ModeratelyActive">{t('profile.activity_moderately')}</option>
                <option value="VeryActive">{t('profile.activity_very')}</option>
                <option value="ExtraActive">{t('profile.activity_extra')}</option>
              </Select>
              {errors.activityLevel && (
                <p className="text-destructive mt-1 text-[11.5px]">
                  {errors.activityLevel.message}
                </p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Body Measurements — heightCm, currentWeightKg, targetWeightKg */}
      <Card>
        <CardHeader>
          <CardTitle className="text-[15px]">{t('profile.measurements_header')}</CardTitle>
          <CardDescription>{t('profile.measurements_desc')}</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-3">
            <div>
              <Label htmlFor="heightCm">{t('profile.height')}</Label>
              <Input
                id="heightCm"
                type="number"
                step="0.1"
                placeholder="e.g., 175"
                className="tnum"
                aria-invalid={!!errors.heightCm}
                {...register('heightCm')}
              />
              {errors.heightCm && (
                <p className="text-destructive mt-1 text-[11.5px]">{errors.heightCm.message}</p>
              )}
            </div>
            <div>
              <Label htmlFor="currentWeightKg">{t('profile.current_weight')}</Label>
              <Input
                id="currentWeightKg"
                type="number"
                step="0.1"
                placeholder="e.g., 75"
                className="tnum"
                aria-invalid={!!errors.currentWeightKg}
                {...register('currentWeightKg')}
              />
              {errors.currentWeightKg && (
                <p className="text-destructive mt-1 text-[11.5px]">
                  {errors.currentWeightKg.message}
                </p>
              )}
            </div>
            <div>
              <Label htmlFor="targetWeightKg">{t('profile.target_weight')}</Label>
              <Input
                id="targetWeightKg"
                type="number"
                step="0.1"
                placeholder="e.g., 70"
                className="tnum"
                aria-invalid={!!errors.targetWeightKg}
                {...register('targetWeightKg')}
              />
              {errors.targetWeightKg && (
                <p className="text-destructive mt-1 text-[11.5px]">
                  {errors.targetWeightKg.message}
                </p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Save */}
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
              {t('profile.save_btn')}
            </>
          )}
        </Button>
      </div>
    </form>
  );
}
