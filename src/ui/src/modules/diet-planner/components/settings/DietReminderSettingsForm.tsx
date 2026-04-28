import { zodResolver } from '@hookform/resolvers/zod';
import {
  useDietReminderSettings,
  useUpdateDietReminderSettings,
} from '@modules/diet-planner/api/hooks/useDietReminderSettings';
import {
  localDayAndTimeToUtc,
  localTimeToUtc,
  utcDayAndTimeToLocal,
  utcTimeToLocal,
} from '@modules/diet-planner/utils/utcTime';
import { Card, CardContent, Button, Input, Label, Checkbox, Select } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Loader2, Save } from 'lucide-react';
import { useEffect } from 'react';
import { useForm, useWatch, Controller } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const MEAL_LEAD_TIME_OPTIONS = [5, 10, 15, 30, 60] as const;
const MEAL_GRACE_OPTIONS = [10, 15, 30, 45, 60, 90] as const;
const WATER_INTERVAL_OPTIONS = [15, 30, 60, 90, 120] as const;
const DAY_OF_WEEK_OPTIONS = [0, 1, 2, 3, 4, 5, 6] as const;

type MealLeadTime = (typeof MEAL_LEAD_TIME_OPTIONS)[number];
type MealGrace = (typeof MEAL_GRACE_OPTIONS)[number];
type WaterInterval = (typeof WATER_INTERVAL_OPTIONS)[number];
type DayOfWeek = (typeof DAY_OF_WEEK_OPTIONS)[number];

const TIME_HHMM = /^([01]\d|2[0-3]):[0-5]\d$/;

const dietReminderSettingsSchema = z
  .object({
    mealRemindersEnabled: z.boolean(),
    mealReminderLeadTimeMinutes: z.coerce
      .number()
      .int()
      .refine((v): v is MealLeadTime => (MEAL_LEAD_TIME_OPTIONS as readonly number[]).includes(v)),
    mealMissedGraceMinutes: z.coerce
      .number()
      .int()
      .refine((v): v is MealGrace => (MEAL_GRACE_OPTIONS as readonly number[]).includes(v)),
    waterRemindersEnabled: z.boolean(),
    waterReminderIntervalMinutes: z.coerce
      .number()
      .int()
      .refine((v): v is WaterInterval => (WATER_INTERVAL_OPTIONS as readonly number[]).includes(v)),
    waterWindowStartLocal: z.string().regex(TIME_HHMM, 'Invalid time'),
    waterWindowEndLocal: z.string().regex(TIME_HHMM, 'Invalid time'),
    weeklySummaryEnabled: z.boolean(),
    weeklySummaryDayOfWeekLocal: z.coerce
      .number()
      .int()
      .refine((v): v is DayOfWeek => (DAY_OF_WEEK_OPTIONS as readonly number[]).includes(v)),
    weeklySummaryTimeOfDayLocal: z.string().regex(TIME_HHMM, 'Invalid time'),
    goalAlertsEnabled: z.boolean(),
  })
  .refine((d) => d.waterWindowEndLocal > d.waterWindowStartLocal, {
    path: ['waterWindowEndLocal'],
    message: 'End must be after start',
  });

type FormInput = z.input<typeof dietReminderSettingsSchema>;
type FormData = z.output<typeof dietReminderSettingsSchema>;

const DEFAULT_VALUES: FormInput = {
  mealRemindersEnabled: true,
  mealReminderLeadTimeMinutes: 15,
  mealMissedGraceMinutes: 30,
  waterRemindersEnabled: true,
  waterReminderIntervalMinutes: 60,
  waterWindowStartLocal: '06:00',
  waterWindowEndLocal: '22:00',
  weeklySummaryEnabled: true,
  weeklySummaryDayOfWeekLocal: 0,
  weeklySummaryTimeOfDayLocal: '08:00',
  goalAlertsEnabled: true,
};

export interface DietReminderSettingsFormProps {
  onSuccess?: () => void;
}

export function DietReminderSettingsForm({ onSuccess }: DietReminderSettingsFormProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const { data: settings, isLoading } = useDietReminderSettings();
  const updateMutation = useUpdateDietReminderSettings();

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { isDirty },
  } = useForm<FormInput, unknown, FormData>({
    resolver: zodResolver(dietReminderSettingsSchema),
    defaultValues: DEFAULT_VALUES,
  });

  const mealRemindersEnabled = useWatch({ control, name: 'mealRemindersEnabled' });
  const waterRemindersEnabled = useWatch({ control, name: 'waterRemindersEnabled' });
  const weeklySummaryEnabled = useWatch({ control, name: 'weeklySummaryEnabled' });

  useEffect(() => {
    if (settings) {
      const inOptions = <T extends number>(opts: readonly T[], v: number, fallback: T): T =>
        (opts as readonly number[]).includes(v) ? (v as T) : fallback;

      const summaryLocal = utcDayAndTimeToLocal(
        settings.weeklySummaryDayOfWeekUtc,
        settings.weeklySummaryTimeOfDayUtc,
      );

      reset({
        mealRemindersEnabled: settings.mealRemindersEnabled,
        mealReminderLeadTimeMinutes: inOptions<MealLeadTime>(
          MEAL_LEAD_TIME_OPTIONS,
          Number(settings.mealReminderLeadTimeMinutes),
          15,
        ),
        mealMissedGraceMinutes: inOptions<MealGrace>(
          MEAL_GRACE_OPTIONS,
          Number(settings.mealMissedGraceMinutes),
          30,
        ),
        waterRemindersEnabled: settings.waterRemindersEnabled,
        waterReminderIntervalMinutes: inOptions<WaterInterval>(
          WATER_INTERVAL_OPTIONS,
          Number(settings.waterReminderIntervalMinutes),
          60,
        ),
        waterWindowStartLocal: utcTimeToLocal(settings.waterWindowStartUtc),
        waterWindowEndLocal: utcTimeToLocal(settings.waterWindowEndUtc),
        weeklySummaryEnabled: settings.weeklySummaryEnabled,
        weeklySummaryDayOfWeekLocal: summaryLocal.localDayOfWeek,
        weeklySummaryTimeOfDayLocal: summaryLocal.localHHmm,
        goalAlertsEnabled: settings.goalAlertsEnabled,
      });
    }
  }, [settings, reset]);

  const onSubmit = async (data: FormData) => {
    const summaryUtc = localDayAndTimeToUtc(
      data.weeklySummaryDayOfWeekLocal,
      data.weeklySummaryTimeOfDayLocal,
    );

    try {
      await updateMutation.mutateAsync({
        mealRemindersEnabled: data.mealRemindersEnabled,
        mealReminderLeadTimeMinutes: data.mealReminderLeadTimeMinutes,
        mealMissedGraceMinutes: data.mealMissedGraceMinutes,
        waterRemindersEnabled: data.waterRemindersEnabled,
        waterReminderIntervalMinutes: data.waterReminderIntervalMinutes,
        waterWindowStartUtc: localTimeToUtc(data.waterWindowStartLocal),
        waterWindowEndUtc: localTimeToUtc(data.waterWindowEndLocal),
        weeklySummaryEnabled: data.weeklySummaryEnabled,
        weeklySummaryDayOfWeekUtc: summaryUtc.dayOfWeekUtc,
        weeklySummaryTimeOfDayUtc: summaryUtc.timeOfDayUtc,
        goalAlertsEnabled: data.goalAlertsEnabled,
      });
      toast.success(t('dietReminderSettings.save_success'));
      onSuccess?.();
    } catch {
      toast.error(t('dietReminderSettings.save_error'));
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-10">
        <Loader2 className="text-muted-foreground h-6 w-6 animate-spin" />
      </div>
    );
  }

  const dayLabel = (d: DayOfWeek): string =>
    t(`dietReminderSettings.day_of_week.${String(d)}`, {
      defaultValue: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'][d] ?? '',
    });

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
              <Checkbox id="mealRemindersEnabled" {...register('mealRemindersEnabled')} />
              <Label htmlFor="mealRemindersEnabled" className="cursor-pointer">
                {t('dietReminderSettings.meal_reminders_enabled_label')}
              </Label>
            </div>
            <div className="w-48 shrink-0">
              <Controller
                control={control}
                name="mealReminderLeadTimeMinutes"
                render={({ field }) => (
                  <Select
                    id="mealReminderLeadTimeMinutes"
                    disabled={!mealRemindersEnabled}
                    aria-label={t('dietReminderSettings.meal_lead_time_label')}
                    value={String(field.value)}
                    onChange={(e) => {
                      field.onChange(Number(e.target.value));
                    }}
                  >
                    {MEAL_LEAD_TIME_OPTIONS.map((minutes) => (
                      <option key={minutes} value={minutes}>
                        {t('dietReminderSettings.minutes_before', {
                          count: minutes,
                          defaultValue: '{{count}} min before',
                        })}
                      </option>
                    ))}
                  </Select>
                )}
              />
            </div>
          </div>

          {/* Meal-missed grace row */}
          <div className="flex items-center justify-between gap-4 rounded-lg px-1 py-3">
            <Label htmlFor="mealMissedGraceMinutes" className="cursor-pointer">
              {t('dietReminderSettings.meal_missed_grace_label')}
            </Label>
            <div className="w-48 shrink-0">
              <Controller
                control={control}
                name="mealMissedGraceMinutes"
                render={({ field }) => (
                  <Select
                    id="mealMissedGraceMinutes"
                    disabled={!mealRemindersEnabled}
                    aria-label={t('dietReminderSettings.meal_missed_grace_label')}
                    value={String(field.value)}
                    onChange={(e) => {
                      field.onChange(Number(e.target.value));
                    }}
                  >
                    {MEAL_GRACE_OPTIONS.map((minutes) => (
                      <option key={minutes} value={minutes}>
                        {t('dietReminderSettings.minutes_after', {
                          count: minutes,
                          defaultValue: '{{count}} min after',
                        })}
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
              <Checkbox id="waterRemindersEnabled" {...register('waterRemindersEnabled')} />
              <Label htmlFor="waterRemindersEnabled" className="cursor-pointer">
                {t('dietReminderSettings.water_reminders_enabled_label')}
              </Label>
            </div>
            <div className="w-48 shrink-0">
              <Controller
                control={control}
                name="waterReminderIntervalMinutes"
                render={({ field }) => (
                  <Select
                    id="waterReminderIntervalMinutes"
                    disabled={!waterRemindersEnabled}
                    aria-label={t('dietReminderSettings.water_interval_label')}
                    value={String(field.value)}
                    onChange={(e) => {
                      field.onChange(Number(e.target.value));
                    }}
                  >
                    {WATER_INTERVAL_OPTIONS.map((minutes) => (
                      <option key={minutes} value={minutes}>
                        {t('dietReminderSettings.every_minutes', {
                          count: minutes,
                          defaultValue: 'every {{count}} min',
                        })}
                      </option>
                    ))}
                  </Select>
                )}
              />
            </div>
          </div>

          {/* Water window row */}
          <div className="grid grid-cols-1 gap-3 px-1 py-3 sm:grid-cols-2">
            <div className="flex flex-col gap-1">
              <Label htmlFor="waterWindowStartLocal">
                {t('dietReminderSettings.water_window_start_label')}
              </Label>
              <Input
                id="waterWindowStartLocal"
                type="time"
                disabled={!waterRemindersEnabled}
                {...register('waterWindowStartLocal')}
              />
            </div>
            <div className="flex flex-col gap-1">
              <Label htmlFor="waterWindowEndLocal">
                {t('dietReminderSettings.water_window_end_label')}
              </Label>
              <Input
                id="waterWindowEndLocal"
                type="time"
                disabled={!waterRemindersEnabled}
                {...register('waterWindowEndLocal')}
              />
            </div>
          </div>

          <div className="border-border border-t" />

          {/* Weekly summary row */}
          <div className="flex items-center gap-3 rounded-lg px-1 py-3">
            <Checkbox id="weeklySummaryEnabled" {...register('weeklySummaryEnabled')} />
            <Label htmlFor="weeklySummaryEnabled" className="cursor-pointer">
              {t('dietReminderSettings.weekly_summary_label')}
            </Label>
          </div>

          <div className="grid grid-cols-1 gap-3 px-1 py-3 sm:grid-cols-2">
            <div className="flex flex-col gap-1">
              <Label htmlFor="weeklySummaryDayOfWeekLocal">
                {t('dietReminderSettings.weekly_summary_day_label')}
              </Label>
              <Controller
                control={control}
                name="weeklySummaryDayOfWeekLocal"
                render={({ field }) => (
                  <Select
                    id="weeklySummaryDayOfWeekLocal"
                    disabled={!weeklySummaryEnabled}
                    value={String(field.value)}
                    onChange={(e) => {
                      field.onChange(Number(e.target.value));
                    }}
                  >
                    {DAY_OF_WEEK_OPTIONS.map((d) => (
                      <option key={d} value={d}>
                        {dayLabel(d)}
                      </option>
                    ))}
                  </Select>
                )}
              />
            </div>
            <div className="flex flex-col gap-1">
              <Label htmlFor="weeklySummaryTimeOfDayLocal">
                {t('dietReminderSettings.weekly_summary_time_label')}
              </Label>
              <Input
                id="weeklySummaryTimeOfDayLocal"
                type="time"
                disabled={!weeklySummaryEnabled}
                {...register('weeklySummaryTimeOfDayLocal')}
              />
            </div>
          </div>

          <div className="border-border border-t" />

          {/* Goal alerts row */}
          <div className="flex items-center gap-3 rounded-lg px-1 py-3">
            <Checkbox id="goalAlertsEnabled" {...register('goalAlertsEnabled')} />
            <Label htmlFor="goalAlertsEnabled" className="cursor-pointer">
              {t('dietReminderSettings.goal_alerts_label')}
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
              {t('dietReminderSettings.save_btn')}
            </>
          )}
        </Button>
      </div>
    </form>
  );
}
