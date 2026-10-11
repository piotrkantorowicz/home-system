import { Banner } from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { cn } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';

import { changeSinceJanuary } from '../lib/netWorth';

import type { HistoryPoint } from '../lib/netWorth';
import type { NetWorth } from '../types';

export interface NetWorthSummaryProps {
  netWorth: NetWorth;
  history: readonly HistoryPoint[] | undefined;
}

/** The total, its change since 1 January, and a warning naming wallets left out for lack of a rate. */
export function NetWorthSummary({ netWorth, history }: NetWorthSummaryProps) {
  const { t, i18n } = useTranslation('investments');
  const { money } = useFormat();
  const change = history ? changeSinceJanuary(history, netWorth.asOf, netWorth.total) : null;
  const missing = netWorth.wallets.filter((w) => w.rateMissing).map((w) => w.name);
  const negative = change?.amount.startsWith('-') ?? false;
  const percent = new Intl.NumberFormat(i18n.language, {
    maximumFractionDigits: 1,
    signDisplay: 'always',
  });

  return (
    <div className="space-y-2">
      <p className="text-muted-foreground text-meta">
        {netWorth.scope === 'mine' ? t('my_net_worth') : t('household_net_worth')}
      </p>
      <p className="text-display tnum font-bold">{money(netWorth.total, { currency: 'PLN' })}</p>
      <p className={cn('text-meta tnum', negative ? 'text-destructive' : 'text-good')}>
        {change
          ? t('change_since_january', {
              amount: `${negative ? '' : '+'}${money(change.amount, { currency: 'PLN' })}`,
              percent: change.percent === null ? '' : ` (${percent.format(change.percent)}%)`,
            })
          : t('change_since_january_unknown')}
      </p>
      {netWorth.incomplete && (
        <Banner variant="warning">
          {t('incomplete_warning', { wallets: missing.join(', ') })}
        </Banner>
      )}
    </div>
  );
}
