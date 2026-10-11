import { useFormat } from '@shared/hooks/useFormat';
import { cn } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';

import { toMinor } from '../lib/money';

import type { NetWorth } from '../types';

// Full class strings so Tailwind sees them; colour always comes with a label in the legend.
const KIND_COLOR: Record<string, string> = {
  Brokerage: 'bg-primary',
  IKE: 'bg-protein',
  IKZE: 'bg-fat',
  PPK: 'bg-carbs',
  TreasuryBonds: 'bg-fiber',
  Savings: 'bg-water',
  Cash: 'bg-muted-foreground',
};

export interface KindBreakdownProps {
  byKind: NetWorth['byKind'];
  total: string;
}

/** Share of net worth per wallet kind: one stacked bar and a labelled legend. */
export function KindBreakdown({ byKind, total }: KindBreakdownProps) {
  const { t } = useTranslation('investments');
  const { money } = useFormat();
  const totalMinor = toMinor(total);
  const parts = byKind
    .filter((k) => toMinor(k.total) > 0)
    .map((k) => ({
      ...k,
      percent: totalMinor > 0 ? (toMinor(k.total) / totalMinor) * 100 : 0,
    }));

  if (parts.length === 0) return null;

  return (
    <section aria-labelledby="kind-breakdown-title" className="space-y-3">
      <h2 id="kind-breakdown-title" className="text-section font-semibold">
        {t('by_kind')}
      </h2>
      <div className="bg-muted flex h-3 overflow-hidden rounded-full" aria-hidden="true">
        {parts.map((p) => (
          <div
            key={p.kind}
            className={cn('h-full', KIND_COLOR[p.kind] ?? 'bg-muted-foreground')}
            style={{ width: `${String(p.percent)}%` }}
          />
        ))}
      </div>
      <ul className="grid gap-2 sm:grid-cols-2">
        {parts.map((p) => (
          <li key={p.kind} className="flex items-center gap-2 text-sm">
            <span
              aria-hidden="true"
              className={cn('size-3 rounded-sm', KIND_COLOR[p.kind] ?? 'bg-muted-foreground')}
            />
            <span className="flex-1">{t(`kind_${p.kind}`)}</span>
            <span className="tnum">{money(p.total, { currency: 'PLN' })}</span>
            <span className="text-muted-foreground tnum w-12 text-right">
              {Math.round(p.percent)}%
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}
