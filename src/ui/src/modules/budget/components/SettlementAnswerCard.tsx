import { Button, MoneyText } from '@shared/components/ui';
import { ArrowRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { Settlement } from '../types';

type Suggestion = Settlement['suggestions'][number];

export interface SettlementAnswerCardProps {
  suggestion: Suggestion;
  currency: string;
  myPersonId: string | null;
  onPay: () => void;
  onDifferentAmount: () => void;
}

function Initial({ name }: { name: string }) {
  return (
    <span
      aria-hidden="true"
      className="bg-muted text-text-2 flex size-10 items-center justify-center rounded-full font-semibold"
    >
      {name.slice(0, 1).toUpperCase()}
    </span>
  );
}

/** One suggested payment: who pays whom, the amount, and a prefilled "I've paid" action. */
export function SettlementAnswerCard({
  suggestion: s,
  currency,
  myPersonId,
  onPay,
  onDifferentAmount,
}: SettlementAnswerCardProps) {
  const { t } = useTranslation('budget');
  const iPay = s.fromPersonId === myPersonId;
  const iReceive = s.toPersonId === myPersonId;
  const headline = iPay
    ? t('you_owe', { name: s.toDisplayName })
    : iReceive
      ? t('owes_you', { name: s.fromDisplayName })
      : t('owes_other', { from: s.fromDisplayName, to: s.toDisplayName });

  return (
    <li className="border-border space-y-3 rounded-xl border p-4">
      <div className="flex items-center gap-3">
        <Initial name={s.fromDisplayName} />
        <ArrowRight aria-hidden="true" className="text-text-2 size-4" />
        <Initial name={s.toDisplayName} />
      </div>
      <p className="text-text-2 text-sm">{headline}</p>
      <MoneyText
        amount={s.amount}
        currency={currency}
        className="block text-4xl leading-tight font-bold"
      />
      <div className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <Button onClick={onPay}>
          {iPay
            ? t('ive_paid', { name: s.toDisplayName })
            : t('record_paid', { from: s.fromDisplayName, to: s.toDisplayName })}
        </Button>
        <Button variant="link" className="h-auto p-0" onClick={onDifferentAmount}>
          {t('different_amount')}
        </Button>
      </div>
    </li>
  );
}
