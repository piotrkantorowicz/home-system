import { useDietReminderSettings } from '@modules/diet-planner/api/hooks/useDietReminderSettings';
import { useHydrationConfig, useWaterDays } from '@modules/diet-planner/api/hooks/useHydration';
import { useWaterActions } from '@modules/diet-planner/api/hooks/useWaterActions';
import { WaterCustomAmountPopover } from '@modules/diet-planner/components/WaterCustomAmountPopover';
import { buildWaterHistory, lastDates } from '@modules/diet-planner/utils/waterHistory';
import { iso } from '@modules/diet-planner/utils/weekHistory';
import {
  Banner,
  Button,
  Card,
  DailyBars,
  EmptyState,
  PageContainer,
  PageHeader,
  Skeleton,
  type DailyBar,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { Droplet, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

const SETTINGS_HREF = '/diet-planner/profile?section=hydration';

/** Quick amounts, each with the vessel it stands for. */
const QUICK = [
  { ml: 250, key: 'quick_glass' },
  { ml: 330, key: 'quick_can' },
  { ml: 500, key: 'quick_bottle' },
] as const;

export default function Hydration() {
  const { t } = useTranslation();
  const fmt = useFormat();
  const today = iso(new Date());
  const dates = lastDates(today, 7);

  const configQuery = useHydrationConfig();
  const reminders = useDietReminderSettings();
  const week = useWaterDays(dates);
  const intakeQuery = week[week.length - 1];
  const { add, remove, pendingKeys } = useWaterActions(today);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);

  const config = configQuery.data;
  const goalMl =
    config?.dailyWaterTargetMl && config.dailyWaterTargetMl > 0 ? config.dailyWaterTargetMl : null;
  const intake = intakeQuery?.data;
  const totalMl = intake?.totalMl ?? 0;
  // Newest first.
  const entries = [...(intake?.entries ?? [])].sort((a, b) =>
    b.timestamp.localeCompare(a.timestamp),
  );

  function confirmRemove(id: string) {
    void remove(id).then((removed) => {
      if (removed) setConfirmDeleteId(null);
    });
  }

  const header = (
    <PageHeader
      title={t('hydration.title')}
      subtitle={fmt.dayShort(today)}
      actions={
        <Button asChild variant="outline" size="xl">
          <Link to={SETTINGS_HREF}>{t('hydration.water_settings')}</Link>
        </Button>
      }
    />
  );

  if (configQuery.isError || intakeQuery?.isError)
    return (
      <PageContainer width="narrow">
        {header}
        <Banner
          variant="error"
          onRetry={() => {
            void configQuery.refetch();
            void intakeQuery?.refetch();
          }}
          retryLabel={t('dashboard.retry')}
        >
          {t('dashboard.data_error')}
        </Banner>
      </PageContainer>
    );

  if (configQuery.isPending || !intakeQuery || intakeQuery.isPending)
    return (
      <PageContainer width="narrow">
        {header}
        <Skeleton className="h-420px rounded-22px w-full" />
      </PageContainer>
    );

  if (config?.trackWaterIntake === false)
    return (
      <PageContainer width="narrow">
        {header}
        <Banner variant="info">
          {t('hydration.tracking_off')}{' '}
          <Link to={SETTINGS_HREF} className="font-semibold underline">
            {t('hydration.water_settings')}
          </Link>
        </Banner>
      </PageContainer>
    );

  const history = buildWaterHistory(
    dates,
    week.map((q) => q.data?.totalMl ?? null),
    goalMl,
  );
  const bars: DailyBar[] = history.days.map((day) => {
    const d = new Date(`${day.date}T00:00:00`);
    return {
      key: day.date,
      label: d.toLocaleDateString(undefined, { weekday: 'short' }),
      sublabel: d.toLocaleDateString(undefined, { day: 'numeric', month: 'numeric' }),
      value: day.totalMl,
      text: day.totalMl === null ? '' : fmt.litres(day.totalMl),
      isToday: day.isToday,
      srText: `${fmt.dayShort(d)}: ${day.totalMl === null ? t('hydration.nothing_logged') : fmt.volume(day.totalMl)}`,
    };
  });

  // "1.3 of 2.5 L": the first token gets the large type.
  const [amount = '', ...rest] =
    goalMl === null
      ? [fmt.volume(totalMl)]
      : fmt.waterProgress(totalMl, goalMl, t('dashboard.water_of')).split(' ');
  const toGoMl = goalMl === null ? 0 : Math.max(0, goalMl - totalMl);

  return (
    <PageContainer width="narrow">
      {header}
      <div className="flex flex-col gap-6">
        <Card className="flex flex-col gap-4 p-6">
          <h2 className="text-label font-semibold">{t('hydration.today_title')}</h2>
          <p className="text-text-2 flex flex-wrap items-baseline gap-x-2 text-sm">
            <strong className="numeral text-foreground text-[44px] leading-none font-semibold">
              {amount}
            </strong>
            <span>{rest.join(' ')}</span>
          </p>
          {goalMl === null ? (
            <p className="text-text-2 text-sm">
              {t('hydration.no_goal')}{' '}
              <Link to={SETTINGS_HREF} className="text-primary font-semibold">
                {t('hydration.water_settings')}
              </Link>
            </p>
          ) : (
            <>
              <div
                role="progressbar"
                aria-label={t('hydration.progress_aria')}
                aria-valuemin={0}
                aria-valuemax={goalMl}
                aria-valuenow={Math.min(totalMl, goalMl)}
                className="bg-muted h-2 overflow-hidden rounded-full"
              >
                <div
                  className="bg-water h-full rounded-full"
                  style={{ width: `${String(Math.min(100, (totalMl / goalMl) * 100))}%` }}
                />
              </div>
              <p className="text-sm font-semibold">
                {toGoMl > 0
                  ? t('hydration.to_go', { amount: fmt.litres(toGoMl) })
                  : t('hydration.goal_reached')}
              </p>
            </>
          )}
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
            {QUICK.map(({ ml, key }) => (
              <Button
                key={ml}
                variant="outline"
                className="h-auto min-h-14 flex-col gap-0 py-2"
                disabled={pendingKeys.has(`add-${String(ml)}`)}
                onClick={() => {
                  add(ml);
                }}
              >
                <span className="numeral font-semibold">+{fmt.volume(ml)}</span>
                <span className="text-text-2 text-label font-normal">{t(`hydration.${key}`)}</span>
              </Button>
            ))}
            <WaterCustomAmountPopover presets={QUICK.map((q) => q.ml)} onAdd={add} />
          </div>

          <div className="flex items-baseline justify-between gap-3 pt-2">
            <h3 className="text-label font-semibold">{t('hydration.entries_header')}</h3>
            <span className="text-text-2 text-label">
              {t('hydration.drinks_count', { count: entries.length })}
            </span>
          </div>
          {entries.length === 0 ? (
            <EmptyState
              icon={Droplet}
              title={t('hydration.no_entries')}
              description={t('hydration.no_entries_desc')}
            />
          ) : (
            <ul className="flex flex-col">
              {entries.map((entry) => (
                <li
                  key={entry.id}
                  className="border-border flex min-h-11 items-center gap-3 border-t first:border-t-0"
                >
                  <span className="text-muted-foreground tnum text-12px w-11 flex-none">
                    {fmt.time(entry.timestamp)}
                  </span>
                  {confirmDeleteId === entry.id ? (
                    <>
                      <span className="text-text-2 min-w-0 flex-1 truncate text-sm">
                        {t('hydration.remove_entry_confirm')}
                      </span>
                      <Button
                        size="xs"
                        variant="outline"
                        onClick={() => {
                          setConfirmDeleteId(null);
                        }}
                      >
                        {t('hydration.remove_entry_no')}
                      </Button>
                      <Button
                        size="xs"
                        variant="destructive"
                        disabled={pendingKeys.has(`remove-${entry.id}`)}
                        onClick={() => {
                          confirmRemove(entry.id);
                        }}
                      >
                        {t('hydration.remove_entry_yes')}
                      </Button>
                    </>
                  ) : (
                    <>
                      <span className="min-w-0 flex-1 truncate text-sm font-semibold">
                        {entry.note ?? t('hydration.entry_water')}
                      </span>
                      <span className="tnum text-sm font-semibold">
                        {fmt.volume(entry.amountMl)}
                      </span>
                      <button
                        type="button"
                        onClick={() => {
                          setConfirmDeleteId(entry.id);
                        }}
                        aria-label={t('hydration.delete_entry_aria')}
                        className="text-muted-foreground hover:text-destructive grid size-11 place-items-center rounded-md"
                      >
                        <Trash2 className="size-4" />
                      </button>
                    </>
                  )}
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card className="flex flex-col gap-4 p-6">
          <h2 className="text-label font-semibold">{t('hydration.history_title')}</h2>
          {week.some((q) => q.isError) ? (
            <Banner
              variant="error"
              onRetry={() => {
                week.forEach((q) => {
                  if (q.isError) void q.refetch();
                });
              }}
              retryLabel={t('dashboard.retry')}
            >
              {t('dashboard.data_error')}
            </Banner>
          ) : week.some((q) => q.isPending) ? (
            <Skeleton className="h-40 w-full" />
          ) : (
            <>
              <DailyBars
                tone="water"
                days={bars}
                target={goalMl}
                {...(goalMl === null
                  ? {}
                  : { targetLabel: t('hydration.goal_line', { amount: fmt.volume(goalMl) }) })}
                ariaLabel={t('hydration.history_title')}
                overLabel=""
                missingText="—"
              />
              {goalMl === null ? null : (
                <p className="text-text-2 text-sm">
                  {t('hydration.goal_met', { met: history.met, total: history.completed })}
                  {history.missing > 0
                    ? ` · ${t('hydration.history_missing', { count: history.missing })}`
                    : ''}
                </p>
              )}
            </>
          )}
        </Card>

        <Card className="flex flex-col gap-3 p-6">
          <h2 className="text-label font-semibold">{t('hydration.setup_title')}</h2>
          <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-3">
            <div>
              <dt className="text-text-2">{t('hydration.setup_goal')}</dt>
              <dd className="font-semibold">{goalMl === null ? '—' : fmt.volume(goalMl)}</dd>
            </div>
            <div>
              <dt className="text-text-2">{t('hydration.setup_glass')}</dt>
              <dd className="font-semibold">{fmt.volume(config?.glassSizeMl)}</dd>
            </div>
            <div>
              <dt className="text-text-2">{t('hydration.setup_reminders')}</dt>
              <dd className="font-semibold">
                {reminders.data
                  ? reminders.data.waterRemindersEnabled
                    ? t('hydration.reminders_every', {
                        count: Number(reminders.data.waterReminderIntervalMinutes),
                      })
                    : t('hydration.reminders_off')
                  : '—'}
              </dd>
            </div>
          </dl>
        </Card>
      </div>
    </PageContainer>
  );
}
