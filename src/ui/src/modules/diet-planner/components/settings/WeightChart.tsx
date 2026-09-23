import { linearRegression } from '@modules/diet-planner/utils/linearRegression';
import { normalizeWeight } from '@modules/diet-planner/utils/normalizeWeight';
import { useTranslation } from 'react-i18next';
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';

import type { WeightEntryDto } from '@modules/diet-planner/api/hooks/useWeightEntries';

export interface WeightChartProps {
  entries: WeightEntryDto[];
  height?: number;
  showTrend?: boolean;
}

interface ChartPoint {
  dateLabel: string;
  dayIndex: number;
  actual: number;
  trend?: number;
}

function buildChartPoints(entries: WeightEntryDto[], locale: string): ChartPoint[] {
  if (entries.length === 0) return [];

  const sorted = [...entries].sort((a, b) => a.date.localeCompare(b.date));
  const first = sorted[0];
  if (!first) return [];
  const baseTime = new Date(first.date).getTime();
  const dayMs = 1000 * 60 * 60 * 24;

  const regression = linearRegression(
    sorted.map((e) => ({
      x: (new Date(e.date).getTime() - baseTime) / dayMs,
      y: normalizeWeight(e.weightKg),
    })),
  );

  const fmt = new Intl.DateTimeFormat(locale, { month: 'short', day: 'numeric' });

  return sorted.map((e): ChartPoint => {
    const dayIndex = (new Date(e.date).getTime() - baseTime) / dayMs;
    const point: ChartPoint = {
      dateLabel: fmt.format(new Date(e.date)),
      dayIndex,
      actual: normalizeWeight(e.weightKg),
    };
    if (regression) {
      point.trend = Math.round((regression.slope * dayIndex + regression.intercept) * 100) / 100;
    }
    return point;
  });
}

export function WeightChart({ entries, height = 280, showTrend = false }: WeightChartProps) {
  const { t, i18n } = useTranslation();

  const data = buildChartPoints(entries, i18n.language);

  if (data.length === 0) {
    return (
      <div
        data-testid="weight-chart-empty"
        className="text-muted-foreground h-280px flex items-center justify-center rounded-lg border border-dashed text-sm"
      >
        {t('weightHistory.chart.empty', 'Log a weight entry to see your trend.')}
      </div>
    );
  }

  return (
    <div data-testid="weight-chart" style={{ height }}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={data} margin={{ top: 10, right: 10, bottom: 10, left: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" />
          <XAxis
            dataKey="dateLabel"
            tick={{ fontSize: 12, fill: 'var(--color-muted-foreground)' }}
            stroke="var(--color-border)"
          />
          <YAxis
            domain={['dataMin - 2', 'dataMax + 2']}
            tick={{ fontSize: 12, fill: 'var(--color-muted-foreground)' }}
            stroke="var(--color-border)"
            tickFormatter={(v: number) => v.toFixed(1)}
          />
          <Tooltip
            contentStyle={{
              background: 'var(--color-card)',
              border: '1px solid var(--color-border)',
              borderRadius: '0.5rem',
              color: 'var(--color-foreground)',
            }}
            formatter={(value, name) => [
              `${typeof value === 'number' ? value.toFixed(1) : String(value)} kg`,
              name === 'actual'
                ? t('weightHistory.chart.actual', 'Weight')
                : t('weightHistory.chart.trend', 'Trend'),
            ]}
          />
          <Line
            type="monotone"
            dataKey="actual"
            stroke="var(--color-primary)"
            strokeWidth={2}
            dot={{ r: 4, fill: 'var(--color-primary)', stroke: 'var(--color-primary)' }}
            activeDot={{ r: 6 }}
            isAnimationActive={false}
          />
          {showTrend && data[0]?.trend !== undefined && (
            <Line
              type="linear"
              dataKey="trend"
              stroke="var(--color-muted-foreground)"
              strokeDasharray="6 4"
              strokeWidth={1.5}
              dot={false}
              isAnimationActive={false}
            />
          )}
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
