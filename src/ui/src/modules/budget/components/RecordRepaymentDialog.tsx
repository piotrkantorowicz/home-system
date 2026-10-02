import { useHousehold } from '@modules/household';
import {
  Banner,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  Field,
  Input,
  Select,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useRepaymentMutation } from '../api/queries';
import { useRequestId } from '../hooks/useRequestId';
import { isIsoDate, todayLocal } from '../lib/dates';
import { normalizeAmount, toMinor } from '../lib/money';

import type { Settlement } from '../types';

export interface RepaymentPrefill {
  fromPersonId: string;
  toPersonId: string;
  amount: string;
}

interface RecordRepaymentDialogProps {
  settlement: Settlement;
  prefill?: RepaymentPrefill;
  onClose: () => void;
}

interface Person {
  id: string;
  name: string;
  former: boolean;
}

export function RecordRepaymentDialog({
  settlement,
  prefill,
  onClose,
}: RecordRepaymentDialogProps) {
  const { t } = useTranslation('budget');
  const toast = useToast();
  const mutation = useRepaymentMutation();
  const requestId = useRequestId();
  const { members, myPersonId } = useHousehold();

  // Current adults, plus anyone who left but still carries a balance.
  const people: Person[] = members
    .filter((m) => m.role === 'Owner' || m.role === 'Adult')
    .map((m) => ({ id: m.personId, name: m.displayName, former: false }));
  settlement.balances.forEach((b) => {
    if (!people.some((p) => p.id === b.personId))
      people.push({ id: b.personId, name: b.displayName, former: true });
  });
  const nameOf = (id: string) => people.find((p) => p.id === id)?.name ?? '';

  const [from, setFrom] = useState(prefill?.fromPersonId ?? myPersonId ?? people[0]?.id ?? '');
  const [to, setTo] = useState(prefill?.toPersonId ?? people.find((p) => p.id !== from)?.id ?? '');
  const [amount, setAmount] = useState(prefill?.amount ?? '');
  const [paidOn, setPaidOn] = useState(todayLocal());
  const [note, setNote] = useState('');
  const [reviewing, setReviewing] = useState(false);
  const [touched, setTouched] = useState(false);

  const normalized = normalizeAmount(amount);
  const errors = {
    to: to === from ? t('repayment_same_person') : undefined,
    amount: normalized ? undefined : t('amount_invalid'),
    paidOn: isIsoDate(paidOn) ? undefined : t('date_invalid'),
  };
  const invalid = !!errors.to || !!errors.amount || !!errors.paidOn || !from || !to;

  // Compared in minor units: an overpayment is allowed, but worth a second look.
  const netOf = (id: string) =>
    toMinor(settlement.balances.find((b) => b.personId === id)?.net ?? '0');
  const limit = Math.min(Math.max(0, -netOf(from)), Math.max(0, netOf(to)));
  const overpays = normalized !== null && toMinor(normalized) > limit;

  const submit = () => {
    if (!normalized) return;
    const input = {
      fromPersonId: from,
      toPersonId: to,
      amount: normalized,
      paidOn,
      note: note.trim() || null,
    };
    mutation.mutate(
      { kind: 'record', requestId: requestId.idFor(JSON.stringify(input)), input },
      {
        onSuccess: () => {
          requestId.reset();
          toast.success(t('repayment_recorded'));
          onClose();
        },
      },
    );
  };

  const options = people.map((p) => (
    <option key={p.id} value={p.id}>
      {p.former ? t('former', { name: p.name }) : p.name}
    </option>
  ));

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent aria-describedby="repayment-description">
        <DialogTitle>{t('record_payment')}</DialogTitle>
        <DialogDescription id="repayment-description">
          {t('repayment_description')}
        </DialogDescription>
        {reviewing ? (
          <div className="space-y-4">
            <p className="font-medium">
              {t('repayment_review', {
                from: nameOf(from),
                to: nameOf(to),
                amount: normalized,
                currency: settlement.currency,
              })}
            </p>
            <p className="text-text-2 text-sm">{t('repayment_no_money')}</p>
            {overpays && <Banner variant="warning">{t('repayment_overpay')}</Banner>}
            {mutation.isError && <Banner variant="error">{t('repayment_save_error')}</Banner>}
            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  setReviewing(false);
                }}
              >
                {t('back')}
              </Button>
              <Button type="button" disabled={mutation.isPending} onClick={submit}>
                {mutation.isPending ? t('saving') : t('repayment_confirm')}
              </Button>
            </div>
          </div>
        ) : (
          <form
            className="space-y-4"
            noValidate
            onSubmit={(event) => {
              event.preventDefault();
              setTouched(true);
              if (!invalid) setReviewing(true);
            }}
          >
            <div className="grid gap-4 sm:grid-cols-2">
              <Field id="repayment-from" label={t('repayment_from')}>
                <Select
                  id="repayment-from"
                  value={from}
                  onChange={(event) => {
                    setFrom(event.target.value);
                  }}
                >
                  {options}
                </Select>
              </Field>
              <Field
                id="repayment-to"
                label={t('repayment_to')}
                error={touched ? errors.to : undefined}
              >
                <Select
                  id="repayment-to"
                  value={to}
                  onChange={(event) => {
                    setTo(event.target.value);
                  }}
                >
                  {options}
                </Select>
              </Field>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field
                id="repayment-amount"
                label={t('amount', { currency: settlement.currency })}
                error={touched ? errors.amount : undefined}
              >
                <Input
                  id="repayment-amount"
                  inputMode="decimal"
                  value={amount}
                  aria-invalid={touched && !!errors.amount}
                  onChange={(event) => {
                    setAmount(event.target.value);
                  }}
                />
              </Field>
              <Field
                id="repayment-date"
                label={t('repayment_date')}
                hint={isIsoDate(paidOn) && paidOn > todayLocal() ? t('date_future') : undefined}
                error={touched ? errors.paidOn : undefined}
              >
                <Input
                  id="repayment-date"
                  type="date"
                  value={paidOn}
                  aria-invalid={touched && !!errors.paidOn}
                  onChange={(event) => {
                    setPaidOn(event.target.value);
                  }}
                />
              </Field>
            </div>
            <Field id="repayment-note" label={t('repayment_note')} hint={t('repayment_note_hint')}>
              <Input
                id="repayment-note"
                value={note}
                maxLength={200}
                onChange={(event) => {
                  setNote(event.target.value);
                }}
              />
            </Field>
            <div className="flex justify-end gap-2">
              <Button type="button" variant="ghost" onClick={onClose}>
                {t('cancel')}
              </Button>
              <Button type="submit">{t('review')}</Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
