import {
  useDietReminderSettings,
  useUpdateDietReminderSettings,
  type DietReminderSettingsDto,
  type DietReminderSettingsRequest,
} from '@modules/diet-planner/api/hooks/useDietReminderSettings';
import { useGoals } from '@modules/diet-planner/api/hooks/useGoals';
import { useProfile } from '@modules/diet-planner/api/hooks/useProfile';
import { useWeightPrediction } from '@modules/diet-planner/api/hooks/useWeightPrediction';
import { Button, Card, MetricTile, Switch } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { cn, formatNumber, formatSigned, getInitials } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

interface ProfileOverviewProps {
  onEdit: (section: 'body-stats' | 'goals' | 'notifications') => void;
}

const ACTIVITY_KEY: Record<string, string> = {
  Sedentary: 'profile.activity_sedentary',
  LightlyActive: 'profile.activity_lightly',
  ModeratelyActive: 'profile.activity_moderately',
  VeryActive: 'profile.activity_very',
  ExtraActive: 'profile.activity_extra',
};

const num = (v: number | string | null | undefined): number =>
  typeof v === 'number' ? v : Number(v ?? 0);

const orNull = (v: number | string | null | undefined): number | null =>
  v === null || v === undefined ? null : num(v);

function ageFrom(dateOfBirth: string | null): number | null {
  if (!dateOfBirth) return null;
  const dob = new Date(dateOfBirth);
  if (Number.isNaN(dob.getTime())) return null;
  const diff = Date.now() - dob.getTime();
  return Math.floor(diff / (365.25 * 24 * 60 * 60 * 1000));
}

export function ProfileOverview({ onEdit }: ProfileOverviewProps) {
  const { t, i18n } = useTranslation();
  const auth = useAuth();

  const { data: profile } = useProfile();
  const { data: goals } = useGoals();
  const { data: prediction } = useWeightPrediction(goals?.dailyCalorieTarget ?? null);

  const displayName =
    auth.user?.profile.name ??
    auth.user?.profile.preferred_username ??
    auth.user?.profile.email ??
    'User';

  const age = ageFrom(profile?.dateOfBirth ?? null);
  const activity = profile?.activityLevel ? t(ACTIVITY_KEY[profile.activityLevel] ?? '') : null;
  const identityMeta = [age !== null ? String(age) : null, activity].filter(Boolean).join(' · ');

  const height = orNull(profile?.heightCm);
  const weight = orNull(profile?.currentWeightKg);
  const targetWeight = orNull(profile?.targetWeightKg);
  const toGo = weight !== null && targetWeight !== null ? Math.abs(weight - targetWeight) : null;

  return (
    <div className="gap-18px grid grid-cols-1 items-start lg:grid-cols-2">
      {/* Identity */}
      <Card className="p-22px flex flex-col gap-4">
        <div className="flex items-center gap-4">
          <div
            className="size-60px text-20px grid flex-none place-items-center rounded-[20px] font-bold text-white"
            style={{ background: 'var(--gradient-avatar)' }}
          >
            {getInitials(displayName)}
          </div>
          <div className="min-w-0">
            <div className="truncate text-[18px] font-bold">{displayName}</div>
            {identityMeta ? (
              <div className="text-muted-foreground text-12-5px">{identityMeta}</div>
            ) : null}
          </div>
        </div>

        <div className="grid grid-cols-3 gap-3">
          <MetricTile
            label={t('profile.height')}
            value={height !== null ? `${formatNumber(height)} cm` : '—'}
          />
          <MetricTile
            label={t('profile.current_weight')}
            value={weight !== null ? `${weight.toFixed(1)} kg` : '—'}
          />
          <MetricTile
            label={t('profile.target_weight')}
            value={targetWeight !== null ? `${targetWeight.toFixed(1)} kg` : '—'}
          />
        </div>

        {toGo !== null ? (
          <div className="flex flex-col gap-1.5">
            <div className="bg-muted h-2.5 overflow-hidden rounded-full">
              <div
                className="h-full rounded-full"
                style={{
                  width: `${String(Math.min(100, Math.max(6, 100 - (toGo / Math.max(toGo + 1, 1)) * 100)))}%`,
                  background: 'var(--gradient-goal)',
                }}
              />
            </div>
            <div className="text-muted-foreground text-11-5px">
              {t('profile.overview.weight_to_go', { kg: toGo.toFixed(1) })}
            </div>
            {prediction?.estimatedGoalDate ? (
              <div className="text-muted-foreground text-11-5px">
                {t('profile.overview.est_goal_date')}{' '}
                <strong className="text-foreground">
                  {new Date(prediction.estimatedGoalDate).toLocaleDateString(i18n.language, {
                    day: 'numeric',
                    month: 'short',
                    year: 'numeric',
                  })}
                </strong>
              </div>
            ) : null}
          </div>
        ) : null}

        <Button
          variant="outline"
          size="xl"
          className="w-full"
          onClick={() => {
            onEdit('body-stats');
          }}
        >
          {t('profile.overview.edit_profile')}
        </Button>
      </Card>

      {/* Energy model */}
      <Card className="p-22px flex flex-col gap-4">
        <div>
          <div className="text-15px font-bold">{t('profile.overview.energy_model')}</div>
          <div className="text-muted-foreground text-12-5px">
            {t('profile.overview.energy_model_sub')}
          </div>
        </div>
        {prediction ? (
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            <MetricTile label="BMR" value={formatNumber(prediction.bmr)} hint="kcal" />
            <MetricTile label="TDEE" value={formatNumber(prediction.tdee)} hint="kcal" />
            <MetricTile
              label={t('profile.overview.daily_deficit')}
              value={formatSigned(-prediction.dailyDeficit)}
              accent="good"
              hint="kcal"
            />
            <MetricTile
              label={t('profile.overview.weekly_change')}
              value={`${formatSigned(prediction.weeklyWeightChange)} kg`}
            />
            <MetricTile
              label={t('profile.overview.current_bmi')}
              value={prediction.currentBmi.toFixed(1)}
            />
            <MetricTile
              label={t('profile.overview.target_bmi')}
              value={prediction.targetBmi !== null ? prediction.targetBmi.toFixed(1) : '—'}
            />
          </div>
        ) : (
          <p className="text-muted-foreground text-12-5px">
            {t('profile.overview.energy_model_empty')}
          </p>
        )}
      </Card>

      {/* Macro targets */}
      <MacroTargetsCard
        goals={goals ?? null}
        onEdit={() => {
          onEdit('goals');
        }}
      />

      {/* Reminders */}
      <RemindersCard
        onEditAll={() => {
          onEdit('notifications');
        }}
      />
    </div>
  );
}

function MacroTargetsCard({
  goals,
  onEdit,
}: {
  goals: {
    proteinGrams: number | null;
    carbsGrams: number | null;
    fatGrams: number | null;
    fiberGrams: number | null;
  } | null;
  onEdit: () => void;
}) {
  const { t } = useTranslation();
  const protein = goals?.proteinGrams ?? 0;
  const carbs = goals?.carbsGrams ?? 0;
  const fat = goals?.fatGrams ?? 0;
  const fiber = goals?.fiberGrams ?? 0;

  const pCal = protein * 4;
  const cCal = carbs * 4;
  const fCal = fat * 9;
  const totalCal = pCal + cCal + fCal;
  const pct = (n: number): number => (totalCal > 0 ? Math.round((n / totalCal) * 100) : 0);

  const rows: {
    key: 'protein' | 'carbs' | 'fat' | 'fiber';
    label: string;
    grams: number;
    share: number | null;
  }[] = [
    { key: 'protein', label: t('dashboard.goal_protein'), grams: protein, share: pct(pCal) },
    { key: 'carbs', label: t('dashboard.goal_carbs'), grams: carbs, share: pct(cCal) },
    { key: 'fat', label: t('dashboard.goal_fat'), grams: fat, share: pct(fCal) },
    { key: 'fiber', label: t('dashboard.goal_fiber'), grams: fiber, share: null },
  ];

  return (
    <Card className="p-22px flex flex-col gap-4">
      <div className="text-15px font-bold">{t('profile.overview.macro_targets')}</div>

      {totalCal > 0 ? (
        <div className="flex h-3 overflow-hidden rounded-full">
          {(['protein', 'carbs', 'fat'] as const).map((key, i) => (
            <div
              key={key}
              style={{
                width: `${String(pct(i === 0 ? pCal : i === 1 ? cCal : fCal))}%`,
                background: `var(--color-${key})`,
              }}
            />
          ))}
        </div>
      ) : (
        <p className="text-muted-foreground text-12-5px">
          {t('profile.overview.macro_targets_empty')}
        </p>
      )}

      <div className="flex flex-col">
        {rows.map((row) => (
          <div
            key={row.key}
            className={cn(
              'text-13px flex items-center justify-between py-2',
              row.key === 'fiber' && 'border-border mt-1 border-t pt-3',
            )}
          >
            <span className="inline-flex items-center gap-2 font-semibold">
              <span
                className="size-2.5 rounded-full"
                style={{ background: `var(--color-${row.key})` }}
              />
              {row.label}
            </span>
            <span className="text-text-2 tnum">
              {formatNumber(row.grams)} g{row.share !== null ? ` · ${String(row.share)}%` : ''}
            </span>
          </div>
        ))}
      </div>

      <Button variant="outline" size="xl" className="w-full" onClick={onEdit}>
        {t('profile.overview.edit_targets')}
      </Button>
    </Card>
  );
}

const num2 = (v: number | string): number => (typeof v === 'number' ? v : Number(v));

function toRequest(s: DietReminderSettingsDto): DietReminderSettingsRequest {
  return {
    mealRemindersEnabled: s.mealRemindersEnabled,
    mealReminderLeadTimeMinutes: num2(s.mealReminderLeadTimeMinutes),
    mealMissedGraceMinutes: num2(s.mealMissedGraceMinutes),
    waterRemindersEnabled: s.waterRemindersEnabled,
    waterReminderIntervalMinutes: num2(s.waterReminderIntervalMinutes),
    waterWindowStart: s.waterWindowStart,
    waterWindowEnd: s.waterWindowEnd,
    weeklySummaryEnabled: s.weeklySummaryEnabled,
    weeklySummaryDayOfWeek: s.weeklySummaryDayOfWeek,
    weeklySummaryTimeOfDay: s.weeklySummaryTimeOfDay,
    goalAlertsEnabled: s.goalAlertsEnabled,
  };
}

function RemindersCard({ onEditAll }: { onEditAll: () => void }) {
  const { t } = useTranslation();
  const toast = useToast();
  const { data: settings } = useDietReminderSettings();
  const update = useUpdateDietReminderSettings();

  const toggle = async (field: keyof DietReminderSettingsRequest, value: boolean) => {
    if (!settings) return;
    try {
      await update.mutateAsync({ ...toRequest(settings), [field]: value });
      toast.success(t('profile.overview.reminder_saved'));
    } catch {
      toast.error(t('profile.overview.reminder_error'));
    }
  };

  const rows: {
    field: keyof DietReminderSettingsRequest;
    label: string;
    detail: string;
    checked: boolean;
  }[] = settings
    ? [
        {
          field: 'mealRemindersEnabled',
          label: t('dietReminderSettings.meal_reminders_enabled_label'),
          detail: t('dietReminderSettings.minutes_before', {
            minutes: num2(settings.mealReminderLeadTimeMinutes),
          }),
          checked: settings.mealRemindersEnabled,
        },
        {
          field: 'waterRemindersEnabled',
          label: t('dietReminderSettings.water_reminders_enabled_label'),
          detail: t('dietReminderSettings.every_minutes', {
            minutes: num2(settings.waterReminderIntervalMinutes),
          }),
          checked: settings.waterRemindersEnabled,
        },
        {
          field: 'weeklySummaryEnabled',
          label: t('dietReminderSettings.weekly_summary_label'),
          detail: String(settings.weeklySummaryDayOfWeek),
          checked: settings.weeklySummaryEnabled,
        },
        {
          field: 'goalAlertsEnabled',
          label: t('dietReminderSettings.goal_alerts_label'),
          detail: '',
          checked: settings.goalAlertsEnabled,
        },
      ]
    : [];

  return (
    <Card className="p-22px flex flex-col gap-2">
      <div className="text-15px mb-1 font-bold">{t('profile.sidebar.notifications')}</div>
      {rows.map((row, i) => (
        <div
          key={row.field}
          className={cn(
            'flex items-center justify-between gap-3 py-2.5',
            i > 0 && 'border-border border-t',
          )}
        >
          <div className="min-w-0">
            <div className="text-13-5px font-semibold">{row.label}</div>
            {row.detail ? (
              <div className="text-muted-foreground text-11-5px">{row.detail}</div>
            ) : null}
          </div>
          <Switch
            checked={row.checked}
            disabled={update.isPending}
            aria-label={row.label}
            onCheckedChange={(v) => {
              void toggle(row.field, v);
            }}
          />
        </div>
      ))}
      <Button variant="outline" size="xl" className="mt-2 w-full" onClick={onEditAll}>
        {t('profile.overview.all_reminder_settings')}
      </Button>
    </Card>
  );
}
