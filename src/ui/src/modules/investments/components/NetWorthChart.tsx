import { useFormat } from '@shared/hooks/useFormat';
import { useTranslation } from 'react-i18next';
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';

import type { HistoryPoint } from '../lib/netWorth';

export interface NetWorthChartProps {
  points: readonly HistoryPoint[];
}

/** Month-end net worth in PLN. Incomplete points are drawn but marked in the tooltip. */
export function NetWorthChart({ points }: NetWorthChartProps) {
  const { t, i18n } = useTranslation('investments');
  const { money } = useFormat();
  const month = new Intl.DateTimeFormat(i18n.language, { month: 'short', timeZone: 'UTC' });
  const data = points.map((p) => ({
    label: month.format(new Date(`${p.date}T00:00:00Z`)),
    total: Number(p.total),
    invested: p.invested ? Number(p.invested) : null,
    point: p,
  }));
  // No contributions anywhere → no invested series.
  const hasInvested = points.some((p) => !!p.invested);

  return (
    <figure aria-label={t('history_chart')} className="h-56">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={data} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" vertical={false} />
          <XAxis
            dataKey="label"
            tick={{ fontSize: 12, fill: 'var(--color-muted-foreground)' }}
            stroke="var(--color-border)"
          />
          <YAxis
            width={64}
            tickFormatter={(v: number) => `${String(Math.round(v / 1000))}k`}
            tick={{ fontSize: 12, fill: 'var(--color-muted-foreground)' }}
            stroke="var(--color-border)"
          />
          <Tooltip
            formatter={(
              _value,
              _name,
              item: { dataKey?: unknown; payload?: { point: HistoryPoint } },
            ) => {
              const point = item.payload?.point;
              if (!point) return '';
              if (item.dataKey === 'invested')
                return point.invested ? money(point.invested, { currency: 'PLN' }) : '—';
              return `${money(point.total, { currency: 'PLN' })}${point.incomplete ? ` · ${t('incomplete_short')}` : ''}`;
            }}
          />
          {hasInvested && <Legend wrapperStyle={{ fontSize: 12 }} />}
          <Line
            type="monotone"
            dataKey="total"
            name={t('net_worth')}
            stroke="var(--color-primary)"
            strokeWidth={2}
            dot={false}
          />
          {hasInvested && (
            <Line
              type="monotone"
              dataKey="invested"
              name={t('invested')}
              stroke="var(--color-text-2)"
              strokeWidth={2}
              strokeDasharray="5 4"
              dot={false}
            />
          )}
        </LineChart>
      </ResponsiveContainer>
    </figure>
  );
}
