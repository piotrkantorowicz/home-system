import { useProfile } from '@modules/diet-planner/api/hooks/useProfile';
import { useWeightEntries } from '@modules/diet-planner/api/hooks/useWeightEntries';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import { Loader2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { WeightChart } from './WeightChart';
import { WeightEntryList } from './WeightEntryList';
import { WeightLogForm } from './WeightLogForm';
import { WeightProgressCard } from './WeightProgressCard';

type Range = '30d' | '90d' | '1y' | 'all';

const RANGES: { id: Range; days: number | null }[] = [
  { id: '30d', days: 30 },
  { id: '90d', days: 90 },
  { id: '1y', days: 365 },
  { id: 'all', days: null },
];

function rangeFromDays(days: number | null): { from?: string; to?: string } {
  if (days === null) return {};
  const to = new Date();
  const from = new Date(to);
  from.setDate(from.getDate() - days);
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) };
}

export function WeightHistorySection() {
  const { t } = useTranslation();
  const [range, setRange] = useState<Range>('90d');
  const [showTrend, setShowTrend] = useState(false);
  const days = RANGES.find((r) => r.id === range)?.days ?? null;
  const { data: profile } = useProfile();
  const queryRange = rangeFromDays(days);
  const { data: entries, isLoading } = useWeightEntries(queryRange);

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">
            {t('weightHistory.log_header', 'Log a weight entry')}
          </CardTitle>
          <CardDescription>
            {t(
              'weightHistory.log_desc',
              'Record your weight for any past date. Logging the same date again replaces the entry.',
            )}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <WeightLogForm />
        </CardContent>
      </Card>

      <WeightProgressCard entries={entries ?? []} targetWeightKg={profile?.targetWeightKg} />

      <Card>
        <CardHeader className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <CardTitle className="text-lg">
              {t('weightHistory.chart_header', 'Weight trend')}
            </CardTitle>
            <CardDescription>
              {t('weightHistory.chart_desc', 'Your weight history over the selected period.')}
            </CardDescription>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <label className="text-muted-foreground flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={showTrend}
                onChange={(e) => {
                  setShowTrend(e.target.checked);
                }}
                className="accent-primary h-4 w-4"
              />
              {t('weightHistory.show_trend', 'Show trend line')}
            </label>
            <div
              className="flex gap-1"
              role="tablist"
              aria-label={t('weightHistory.range_label', 'Range')}
            >
              {RANGES.map((r) => (
                <button
                  key={r.id}
                  type="button"
                  role="tab"
                  aria-selected={range === r.id}
                  onClick={() => {
                    setRange(r.id);
                  }}
                  className={cn(
                    'rounded-md px-3 py-1 text-sm transition-colors',
                    range === r.id
                      ? 'bg-primary text-primary-foreground'
                      : 'text-muted-foreground hover:bg-accent hover:text-foreground',
                  )}
                >
                  {t(`weightHistory.range.${r.id}`, r.id)}
                </button>
              ))}
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex h-[280px] items-center justify-center">
              <Loader2 className="text-muted-foreground h-6 w-6 animate-spin" />
            </div>
          ) : (
            <WeightChart entries={entries ?? []} showTrend={showTrend} />
          )}
        </CardContent>
      </Card>

      {entries && entries.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">
              {t('weightHistory.entries_header', 'Recent entries')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <WeightEntryList entries={entries} />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
