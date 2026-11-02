import { useFormat } from '@shared/hooks/useFormat';
import { cn } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';

import { toMinor } from '../lib/money';
import { gainPercent } from '../lib/netWorth';

export interface GainProps {
  /** API decimal string, signed. */
  amount: string;
  currency: string;
  /** What the gain is measured against; adds a percent when positive. */
  invested?: string;
  className?: string;
}

/** A signed gain: `+` and the good colour above zero, the destructive colour below, neutral at zero. */
export function Gain({ amount, currency, invested, className }: GainProps) {
  const { i18n } = useTranslation();
  const { money } = useFormat();
  const minor = toMinor(amount);
  const percent = invested === undefined ? null : gainPercent(amount, invested);
  const format = new Intl.NumberFormat(i18n.language, {
    maximumFractionDigits: 1,
    signDisplay: 'exceptZero',
  });

  return (
    <span
      className={cn(
        'tnum',
        minor > 0 ? 'text-good' : minor < 0 ? 'text-destructive' : 'text-text-2',
        className,
      )}
    >
      {`${minor > 0 ? '+' : ''}${money(amount, { currency })}`}
      {percent !== null && ` (${format.format(percent)}%)`}
    </span>
  );
}
