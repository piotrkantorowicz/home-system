import { zodResolver } from '@hookform/resolvers/zod';
import {
  useNotificationPreferences,
  useUpdateNotificationPreferences,
} from '@modules/diet-planner/api/hooks/useNotificationPreferences';
import {
  Card,
  CardContent,
  Button,
  Label,
  Checkbox,
  Select,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Loader2, Save } from 'lucide-react';
import { useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const MEAL_LEAD_TIME_OPTIONS = [5, 10, 15, 30, 60] as const;
const WATER_INTERVAL_OPTIONS = [15, 30, 60, 90, 120] as const;

type MealLeadTime = (typeof MEAL_LEAD_TIME_OPTIONS)[number];
type WaterInterval = (typeof WATER_INTERVAL_OPTIONS)[number];

const notificationPrefsSchema = z.object({
  mealReminderEnabled: z.boolean(),
  mealReminderLeadTimeMinutes: z.coerce
    .number()
    .int()
    .refine((v): v is MealLeadTime => (MEAL_LEAD_TIME_OPTIONS as readonly number[]).includes(v)),
  waterReminderEnabled: z.boolean(),
  waterReminderIntervalMinutes: z.coerce
    .number()
    .int()
    .refine((v): v is WaterInterval => (WATER_INTERVAL_OPTIONS as readonly number[]).includes(v)),
  weeklySummaryEnabled: z.boolean(),
  goalMilestoneAlertsEnabled: z.boolean(),
});

type NotificationPrefsFormInput = z.input<typeof notificationPrefsSchema>;
type NotificationPrefsFormData = z.output<typeof notificationPrefsSchema>;

const DEFAULT_VALUES: NotificationPrefsFormInput = {
  mealReminderEnabled: true,
  mealReminderLeadTimeMinutes: 15,
  waterReminderEnabled: true,
  waterReminderIntervalMinutes: 60,
  weeklySummaryEnabled: true,
  goalMilestoneAlertsEnabled: true,
};

export interface NotificationPrefsFormProps {
  onSuccess?: () => void;
}

export function NotificationPrefsForm({ onSuccess }: NotificationPrefsFormProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const { data: preferences, isLoading } = useNotificationPreferences();
  const updateMutation = useUpdateNotificationPreferences();

  const {
    register,
    handleSubmit,
    reset,
    watch,
    control,
    formState: { isDirty },
  } = useForm<NotificationPrefsFormInput, unknown, NotificationPrefsFormData>({
    resolver: zodResolver(notificationPrefsSchema),
    defaultValues: DEFAULT_VALUES,
  });

  const mealReminderEnabled = watch('mealReminderEnabled');
  const waterReminderEnabled = watch('waterReminderEnabled');

  useEffect(() => {
    if (preferences) {
      const mealLeadTime = (MEAL_LEAD_TIME_OPTIONS as readonly number[]).includes(
        preferences.mealReminderLeadTimeMinutes,
      )
        ? preferences.mealReminderLeadTimeMinutes
        : DEFAULT_VALUES.mealReminderLeadTimeMinutes;

      const waterInterval = (WATER_INTERVAL_OPTIONS as readonly number[]).includes(
        preferences.waterReminderIntervalMinutes,
      )
        ? preferences.waterReminderIntervalMinutes
        : DEFAULT_VALUES.waterReminderIntervalMinutes;

      reset({
        mealReminderEnabled: preferences.mealReminderEnabled,
        mealReminderLeadTimeMinutes: mealLeadTime,
        waterReminderEnabled: preferences.waterReminderEnabled,
        waterReminderIntervalMinutes: waterInterval,
        weeklySummaryEnabled: preferences.weeklySummaryEnabled,
        goalMilestoneAlertsEnabled: preferences.goalMilestoneAlertsEnabled,
      });
    }
  }, [preferences, reset]);

  const onSubmit = async (data: NotificationPrefsFormData) => {
    try {
      await updateMutation.mutateAsync(data);
      toast.success(t('notifications.save_success'));
      onSuccess?.();
    } catch {
      toast.error(t('notifications.save_error'));
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
      noValidate
      className="space-y-6"
    >
      <Card>
        <CardContent className="space-y-1 pt-6">
          {/* Meal reminder row */}
          <div className="flex items-center justify-between gap-4 rounded-lg px-1 py-3">
            <div className="flex items-center gap-3">
              <Checkbox id="mealReminderEnabled" {...register('mealReminderEnabled')} />
              <Label htmlFor="mealReminderEnabled" className="cursor-pointer">
                {t('notifications.meal_reminder_enabled_label')}
              </Label>
            </div>
            <div className="w-48 shrink-0">
              <Controller
                control={control}
                name="mealReminderLeadTimeMinutes"
                render={({ field }) => (
                  <Select
                    id="mealReminderLeadTimeMinutes"
                    disabled={!mealReminderEnabled}
                    aria-label={t('notifications.meal_lead_time_label')}
                    value={String(field.value)}
                    onChange={(e) => {
                      field.onChange(Number(e.target.value));
                    }}
                  >
                    {MEAL_LEAD_TIME_OPTIONS.map((minutes) => (
                      <option key={minutes} value={minutes}>
                        {/* TODO(Task 14): replace with t('notifications.minutes_before', { count: minutes }) */}
                        {`${String(minutes)} min before`}
                      </option>
                    ))}
                  </Select>
                )}
              />
            </div>
          </div>

          <div className="border-border border-t" />

          {/* Water reminder row */}
          <div className="flex items-center justify-between gap-4 rounded-lg px-1 py-3">
            <div className="flex items-center gap-3">
              <Checkbox id="waterReminderEnabled" {...register('waterReminderEnabled')} />
              <Label htmlFor="waterReminderEnabled" className="cursor-pointer">
                {t('notifications.water_reminder_enabled_label')}
              </Label>
            </div>
            <div className="w-48 shrink-0">
              <Controller
                control={control}
                name="waterReminderIntervalMinutes"
                render={({ field }) => (
                  <Select
                    id="waterReminderIntervalMinutes"
                    disabled={!waterReminderEnabled}
                    aria-label={t('notifications.water_interval_label')}
                    value={String(field.value)}
                    onChange={(e) => {
                      field.onChange(Number(e.target.value));
                    }}
                  >
                    {WATER_INTERVAL_OPTIONS.map((minutes) => (
                      <option key={minutes} value={minutes}>
                        {/* TODO(Task 14): replace with t('notifications.every_minutes', { count: minutes }) */}
                        {`Every ${String(minutes)} min`}
                      </option>
                    ))}
                  </Select>
                )}
              />
            </div>
          </div>

          <div className="border-border border-t" />

          {/* Weekly summary row */}
          <div className="flex items-center gap-3 rounded-lg px-1 py-3">
            <Checkbox id="weeklySummaryEnabled" {...register('weeklySummaryEnabled')} />
            <Label htmlFor="weeklySummaryEnabled" className="cursor-pointer">
              {t('notifications.weekly_summary_label')}
            </Label>
          </div>

          <div className="border-border border-t" />

          {/* Goal milestone row */}
          <div className="flex items-center gap-3 rounded-lg px-1 py-3">
            <Checkbox id="goalMilestoneAlertsEnabled" {...register('goalMilestoneAlertsEnabled')} />
            <Label htmlFor="goalMilestoneAlertsEnabled" className="cursor-pointer">
              {t('notifications.goal_milestone_label')}
            </Label>
          </div>
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
              {t('notifications.save_btn')}
            </>
          )}
        </Button>
      </div>
    </form>
  );
}
