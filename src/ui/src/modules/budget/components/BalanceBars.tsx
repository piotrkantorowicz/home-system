import { Badge, MoneyText } from '@shared/components/ui';
import { useTranslation } from 'react-i18next';

import { minorToDecimal, toMinor } from '../lib/money';

import type { Settlement } from '../types';

/** Per-person balance on a bar around zero. Neutral colours: owing is not an error. */
export function BalanceBars({ balances, currency }: Pick<Settlement, 'balances' | 'currency'>) {
  const { t } = useTranslation('budget');
  const max = Math.max(1, ...balances.map((b) => Math.abs(toMinor(b.net))));
  return (
    <ul className="space-y-3">
      {balances.map((b) => {
        const net = toMinor(b.net);
        const width = `${String((Math.abs(net) / max) * 50)}%`;
        return (
          <li key={b.personId} className="space-y-1">
            <div className="flex flex-wrap items-baseline justify-between gap-x-3 text-sm">
              <span className="font-semibold break-words">
                {b.displayName}
                {b.isFormerAdult && (
                  <Badge variant="outline" className="ml-2">
                    {t('former_adult')}
                  </Badge>
                )}
              </span>
              <span className="text-text-2">
                {net === 0 ? (
                  t('balance_zero')
                ) : (
                  <>
                    {t(net < 0 ? 'balance_owes' : 'balance_gets')}{' '}
                    <MoneyText amount={minorToDecimal(Math.abs(net))} currency={currency} />
                  </>
                )}
              </span>
            </div>
            <div aria-hidden="true" className="bg-muted relative h-2 rounded-full">
              <span className="bg-border absolute inset-y-0 left-1/2 w-px" />
              <span
                className="bg-text-2 absolute inset-y-0 rounded-full"
                style={net < 0 ? { right: '50%', width } : { left: '50%', width }}
              />
            </div>
          </li>
        );
      })}
    </ul>
  );
}
