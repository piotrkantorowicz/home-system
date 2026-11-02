import { Banner, Button, Field, Input, Select } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useContributionMutation } from '../api/queries';
import { EARLIEST_AS_OF, todayInWarsaw } from '../lib/dates';
import { normalizeAmount, toMinor } from '../lib/money';
import { CONTRIBUTION_DIRECTIONS } from '../types';

import type { ContributionDirection, Wallet } from '../types';

const NOTE_MAX = 200;

export interface RecordContributionFormProps {
  wallet: Wallet;
}

/** Record money put into or taken out of a wallet; several per date are allowed. */
export function RecordContributionForm({ wallet }: RecordContributionFormProps) {
  const { t } = useTranslation('investments');
  const toast = useToast();
  const mutation = useContributionMutation();
  const today = todayInWarsaw();
  const [date, setDate] = useState(today);
  const [direction, setDirection] = useState<ContributionDirection>('Deposit');
  const [amount, setAmount] = useState('');
  const [note, setNote] = useState('');
  const [amountError, setAmountError] = useState<string | null>(null);
  const [dateError, setDateError] = useState<string | null>(null);

  const handleSubmit = (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    const normalized = normalizeAmount(amount);
    const amountOk = normalized !== null && toMinor(normalized) > 0;
    const dateOk = date >= EARLIEST_AS_OF && date <= today;
    setAmountError(amountOk ? null : t('contribution_amount_invalid'));
    setDateError(dateOk ? null : t('date_invalid'));
    if (normalized === null || !amountOk || !dateOk) return;
    mutation.mutate(
      {
        kind: 'record',
        walletId: wallet.id,
        date,
        direction,
        amount: normalized,
        note: note.trim() || null,
      },
      {
        onSuccess: () => {
          toast.success(t('contribution_saved'));
          setAmount('');
          setNote('');
        },
      },
    );
  };

  return (
    <form
      className="space-y-4"
      noValidate
      onSubmit={handleSubmit}
      aria-labelledby="contribution-title"
    >
      <h2 id="contribution-title" className="text-section font-semibold">
        {t('record_contribution')}
      </h2>
      <div className="grid gap-4 sm:grid-cols-[auto_auto_1fr]">
        <Field id="contribution-date" label={t('date')} error={dateError ?? undefined}>
          <Input
            id="contribution-date"
            type="date"
            min={EARLIEST_AS_OF}
            max={today}
            value={date}
            aria-invalid={dateError !== null}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
              setDate(e.target.value);
            }}
          />
        </Field>
        <Field id="contribution-direction" label={t('direction')}>
          <Select
            id="contribution-direction"
            value={direction}
            onChange={(e: React.ChangeEvent<HTMLSelectElement>) => {
              setDirection(e.target.value as ContributionDirection);
            }}
          >
            {CONTRIBUTION_DIRECTIONS.map((value) => (
              <option key={value} value={value}>
                {t(`direction_${value}`)}
              </option>
            ))}
          </Select>
        </Field>
        <Field
          id="contribution-amount"
          label={t('amount_in', { currency: wallet.currency })}
          error={amountError ?? undefined}
        >
          <Input
            id="contribution-amount"
            inputMode="decimal"
            autoComplete="off"
            value={amount}
            aria-invalid={amountError !== null}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
              setAmount(e.target.value);
            }}
          />
        </Field>
      </div>
      <Field id="contribution-note" label={t('note_optional')}>
        <Input
          id="contribution-note"
          maxLength={NOTE_MAX}
          value={note}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
            setNote(e.target.value);
          }}
        />
      </Field>
      {mutation.isError && <Banner variant="error">{t('contribution_save_error')}</Banner>}
      <div className="flex justify-end">
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? t('saving') : t('save_contribution')}
        </Button>
      </div>
    </form>
  );
}
