import { zodResolver } from '@hookform/resolvers/zod';
import {
  useNotificationPreferences,
  useUpdateNotificationPreferences,
} from '@modules/diet-planner/api/hooks/useNotificationPreferences';
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
import { Bell, Loader2, Save } from 'lucide-react';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const notificationPreferencesSchema = z.object({
  mealReminderEnabled: z.boolean(),
  mealReminderLeadTimeMinutes: z.coerce.number().int().min(1).max(180),
  waterReminderEnabled: z.boolean(),
  waterReminderIntervalMinutes: z.coerce.number().int().min(15).max(480),
  weeklySummaryEnabled: z.boolean(),
  goalMilestoneAlertsEnabled: z.boolean(),
});

type NotificationPreferencesFormInput = z.input<typeof notificationPreferencesSchema>;
type NotificationPreferencesFormData = z.output<typeof notificationPreferencesSchema>;

const DEFAULT_VALUES: NotificationPreferencesFormInput = {
  mealReminderEnabled: true,
  mealReminderLeadTimeMinutes: 15,
  waterReminderEnabled: true,
  waterReminderIntervalMinutes: 60,
  weeklySummaryEnabled: true,
  goalMilestoneAlertsEnabled: true,
};

export default function NotificationPreferences() {
  const { t } = useTranslation();
  const { data: preferences, isLoading } = useNotificationPreferences();
  const updateMutation = useUpdateNotificationPreferences();

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isDirty },
  } = useForm<NotificationPreferencesFormInput, unknown, NotificationPreferencesFormData>({
    resolver: zodResolver(notificationPreferencesSchema),
    defaultValues: DEFAULT_VALUES,
  });

  const mealReminderEnabled = watch('mealReminderEnabled');
  const waterReminderEnabled = watch('waterReminderEnabled');

  useEffect(() => {
    if (preferences) {
      reset({
        mealReminderEnabled: preferences.mealReminderEnabled,
        mealReminderLeadTimeMinutes: preferences.mealReminderLeadTimeMinutes,
        waterReminderEnabled: preferences.waterReminderEnabled,
        waterReminderIntervalMinutes: preferences.waterReminderIntervalMinutes,
        weeklySummaryEnabled: preferences.weeklySummaryEnabled,
        goalMilestoneAlertsEnabled: preferences.goalMilestoneAlertsEnabled,
      });
    }
  }, [preferences, reset]);

  const onSubmit = async (data: NotificationPreferencesFormData) => {
    await updateMutation.mutateAsync(data);
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
            <Bell className="h-6 w-6 text-blue-600 dark:text-blue-400" />
          </div>
          <h1 className="text-3xl font-bold tracking-tight">{t('notifications.title')}</h1>
        </div>
        <p className="text-muted-foreground">{t('notifications.subtitle')}</p>
      </div>

      <form
        onSubmit={(e) => {
          void handleSubmit(onSubmit)(e);
        }}
        noValidate
        className="space-y-6"
      >
        {/* Meal Reminders */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('notifications.meal_reminders_header')}</CardTitle>
            <CardDescription>{t('notifications.meal_reminders_desc')}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-3">
              <input
                id="mealReminderEnabled"
                type="checkbox"
                className="h-4 w-4 rounded border-gray-300 accent-blue-600"
                {...register('mealReminderEnabled')}
              />
              <Label htmlFor="mealReminderEnabled">
                {t('notifications.meal_reminder_enabled_label')}
              </Label>
            </div>
            <div className="max-w-xs">
              <Label htmlFor="mealReminderLeadTimeMinutes">
                {t('notifications.meal_lead_time_label')}
              </Label>
              <Input
                id="mealReminderLeadTimeMinutes"
                type="number"
                step="1"
                min="1"
                max="180"
                disabled={!mealReminderEnabled}
                {...register('mealReminderLeadTimeMinutes')}
              />
              {errors.mealReminderLeadTimeMinutes && (
                <p className="text-destructive mt-1 text-sm">
                  {errors.mealReminderLeadTimeMinutes.message}
                </p>
              )}
              <p className="text-muted-foreground mt-1 text-xs">
                {t('notifications.meal_lead_time_help')}
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Water Reminders */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('notifications.water_reminders_header')}</CardTitle>
            <CardDescription>{t('notifications.water_reminders_desc')}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-3">
              <input
                id="waterReminderEnabled"
                type="checkbox"
                className="h-4 w-4 rounded border-gray-300 accent-blue-600"
                {...register('waterReminderEnabled')}
              />
              <Label htmlFor="waterReminderEnabled">
                {t('notifications.water_reminder_enabled_label')}
              </Label>
            </div>
            <div className="max-w-xs">
              <Label htmlFor="waterReminderIntervalMinutes">
                {t('notifications.water_interval_label')}
              </Label>
              <Input
                id="waterReminderIntervalMinutes"
                type="number"
                step="1"
                min="15"
                max="480"
                disabled={!waterReminderEnabled}
                {...register('waterReminderIntervalMinutes')}
              />
              {errors.waterReminderIntervalMinutes && (
                <p className="text-destructive mt-1 text-sm">
                  {errors.waterReminderIntervalMinutes.message}
                </p>
              )}
              <p className="text-muted-foreground mt-1 text-xs">
                {t('notifications.water_interval_help')}
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Other Notifications */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">{t('notifications.other_header')}</CardTitle>
            <CardDescription>{t('notifications.other_desc')}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-3">
              <input
                id="weeklySummaryEnabled"
                type="checkbox"
                className="h-4 w-4 rounded border-gray-300 accent-blue-600"
                {...register('weeklySummaryEnabled')}
              />
              <Label htmlFor="weeklySummaryEnabled">
                {t('notifications.weekly_summary_label')}
              </Label>
            </div>
            <div className="flex items-center gap-3">
              <input
                id="goalMilestoneAlertsEnabled"
                type="checkbox"
                className="h-4 w-4 rounded border-gray-300 accent-blue-600"
                {...register('goalMilestoneAlertsEnabled')}
              />
              <Label htmlFor="goalMilestoneAlertsEnabled">
                {t('notifications.goal_milestone_label')}
              </Label>
            </div>
          </CardContent>
        </Card>

        {/* Submit */}
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
                {t('notifications.save_btn')}
              </>
            )}
          </Button>
        </div>

        {updateMutation.isSuccess && (
          <p className="text-right text-sm text-emerald-600 dark:text-emerald-400">
            {t('notifications.save_success')}
          </p>
        )}
      </form>
    </div>
  );
}
