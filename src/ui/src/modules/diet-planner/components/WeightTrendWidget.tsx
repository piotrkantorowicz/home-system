import { useProfile } from '@modules/diet-planner/api/hooks/useProfile';
import {
  useWeightEntries,
  type WeightEntryDto,
} from '@modules/diet-planner/api/hooks/useWeightEntries';
import { WeightLogForm } from '@modules/diet-planner/components/settings/WeightLogForm';
import { normalizeWeight } from '@modules/diet-planner/utils/normalizeWeight';
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@shared/components/ui';
import { ArrowDown, ArrowUp, Minus, Plus } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { Line, LineChart, ResponsiveContainer } from 'recharts';

const DAYS = 30;

function getRange(): { from: string; to: string } {
  const to = new Date();
  const from = new Date(to);
  from.setDate(from.getDate() - DAYS);
  return { from: from.toISOString().slice(0, 10), to: to.toISOString().slice(0, 10) };
}

function computeDelta(entries: WeightEntryDto[]): { current: number; delta: number } | null {
  if (entries.length === 0) return null;
  const sorted = [...entries].sort((a, b) => a.date.localeCompare(b.date));
  const first = sorted[0];
  const last = sorted[sorted.length - 1];
  if (!first || !last) return null;
  const current = normalizeWeight(last.weightKg);
  const earliest = normalizeWeight(first.weightKg);
  return { current, delta: current - earliest };
}

export function WeightTrendWidget() {
  const { t } = useTranslation();
  const { data: profile } = useProfile();
  const range = getRange();
  const { data: entries } = useWeightEntries(range);
  const [logOpen, setLogOpen] = useState(false);

  if (!profile) return null;

  const summary = entries ? computeDelta(entries) : null;
  const target = profile.targetWeightKg !== null ? normalizeWeight(profile.targetWeightKg) : null;
  const sortedEntries = entries ? [...entries].sort((a, b) => a.date.localeCompare(b.date)) : [];

  let deltaColor = 'text-muted-foreground';
  let DeltaIcon = Minus;
  if (summary && Math.abs(summary.delta) >= 0.1) {
    const movingTowardsTarget =
      target === null
        ? false
        : (target < summary.current && summary.delta < 0) ||
          (target > summary.current && summary.delta > 0);
    deltaColor = movingTowardsTarget ? 'text-emerald-500' : 'text-rose-500';
    DeltaIcon = summary.delta < 0 ? ArrowDown : ArrowUp;
  }

  return (
    <>
      <Card data-testid="weight-trend-widget">
        <CardHeader>
          <CardTitle className="text-lg">
            {t('weightHistory.widget.title', 'Weight trend')}
          </CardTitle>
          <CardDescription>
            {t('weightHistory.widget.subtitle', 'Last {{days}} days', { days: DAYS })}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {summary ? (
            <>
              <div className="mb-3 flex items-baseline justify-between">
                <span className="text-2xl font-bold">{summary.current.toFixed(1)} kg</span>
                <span className={`flex items-center gap-1 text-sm font-medium ${deltaColor}`}>
                  <DeltaIcon className="h-4 w-4" />
                  {summary.delta > 0 ? '+' : ''}
                  {summary.delta.toFixed(1)} kg
                </span>
              </div>
              <div className="h-16" data-testid="weight-trend-sparkline">
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={sortedEntries}>
                    <Line
                      type="monotone"
                      dataKey="weightKg"
                      stroke="var(--color-primary)"
                      strokeWidth={2}
                      dot={{ r: 2, fill: 'var(--color-primary)' }}
                      isAnimationActive={false}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </>
          ) : (
            <div data-testid="weight-trend-empty" className="text-muted-foreground py-4 text-sm">
              {t('weightHistory.widget.empty', 'No weight logged yet for the last 30 days.')}
            </div>
          )}

          <div className="mt-4 flex items-center justify-between gap-2">
            <Button
              type="button"
              size="sm"
              onClick={() => {
                setLogOpen(true);
              }}
            >
              <Plus className="mr-1 h-4 w-4" />
              {t('weightHistory.widget.log', 'Log weight')}
            </Button>
            <Link
              to="/diet-planner/profile?section=weight-history"
              className="text-primary text-sm hover:underline"
            >
              {t('weightHistory.widget.view_history', 'View history')}
            </Link>
          </div>
        </CardContent>
      </Card>

      <Dialog open={logOpen} onOpenChange={setLogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('weightHistory.widget.log_dialog_title', 'Log weight')}</DialogTitle>
            <DialogDescription>
              {t('weightHistory.widget.log_dialog_desc', 'Record your weight for any past date.')}
            </DialogDescription>
          </DialogHeader>
          <WeightLogForm
            onSuccess={() => {
              setLogOpen(false);
            }}
          />
        </DialogContent>
      </Dialog>
    </>
  );
}
