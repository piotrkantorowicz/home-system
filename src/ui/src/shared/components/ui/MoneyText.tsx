import { useFormat } from '@shared/hooks/useFormat';

import { Num } from './Num';

export interface MoneyTextProps {
  /** API decimal string, e.g. `"4114.65"`; the currency is shown once by the caller. */
  amount: string | null | undefined;
  currency?: string;
  className?: string;
}

/** Money in the active language's grouping and decimal mark, in tabular figures. */
export function MoneyText({ amount, currency, className }: MoneyTextProps) {
  const { money } = useFormat();
  return <Num className={className}>{money(amount, currency ? { currency } : undefined)}</Num>;
}
