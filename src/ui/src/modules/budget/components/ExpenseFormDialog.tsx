import { zodResolver } from '@hookform/resolvers/zod';
import { useHousehold } from '@modules/household';
import {
  Banner,
  Button,
  Checkbox,
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
import { useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

import { errorStatus } from '../api/client';
import {
  useAccountsQuery,
  useBudgetQuery,
  useExpenseMutation,
  useExpensesQuery,
} from '../api/queries';
import { useRequestId } from '../hooks/useRequestId';
import { isIsoDate, shiftDays, todayLocal } from '../lib/dates';
import { normalizeAmount } from '../lib/money';
import { EXPENSE_CATEGORIES } from '../types';

import type { Expense, ExpenseCategory, ExpenseInput, FundingSource } from '../types';

interface ExpenseFormDialogProps {
  /** Present when correcting an expense; absent when recording a new one. */
  expense?: Expense;
  onClose: () => void;
  /** Correction conflict: reload the latest version (the caller refetches and closes this form). */
  onReload?: () => void;
}

interface Person {
  id: string;
  name: string;
  former: boolean;
}

const MAX_REASON = 200;

export function ExpenseFormDialog({ expense, onClose, onReload }: ExpenseFormDialogProps) {
  const { t } = useTranslation('budget');
  const toast = useToast();
  const mutation = useExpenseMutation();
  const requestId = useRequestId();
  const { members, myPersonId } = useHousehold();
  const budget = useBudgetQuery();
  const accountsQuery = useAccountsQuery();
  const editing = expense !== undefined;

  const accounts = (accountsQuery.data ?? []).filter((a) =>
    editing ? a.id === expense.accountId : !a.isArchived,
  );

  // Current adults, plus anyone already on the expense who has since left or been demoted.
  const people: Person[] = members
    .filter((m) => m.role === 'Owner' || m.role === 'Adult')
    .map((m) => ({ id: m.personId, name: m.displayName, former: false }));
  const addFormer = (id: string | null | undefined, name: string | null | undefined) => {
    if (id && !people.some((p) => p.id === id)) people.push({ id, name: name ?? '', former: true });
  };
  if (expense) {
    addFormer(expense.paidByPersonId, expense.paidByDisplayName);
    expense.shares.forEach((s) => {
      addFormer(s.personId, s.personDisplayName);
    });
  }
  const adultIds = people.filter((p) => !p.former).map((p) => p.id);

  const initialAccount =
    expense?.accountId ??
    accounts.find((a) => a.visibility === 'Household')?.id ??
    accounts[0]?.id ??
    '';
  const schema = z
    .object({
      amount: z.string().refine((v) => normalizeAmount(v) !== null, t('amount_invalid')),
      occurredOn: z.string().refine(isIsoDate, t('date_invalid')),
      category: z.enum(EXPENSE_CATEGORIES),
      accountId: z.string().min(1, t('envelope_required')),
      fundingSource: z.enum(['Individual', 'HouseholdFunds']),
      paidByPersonId: z.string(),
      participantIds: z.array(z.string()),
      reason: z.string().trim().max(MAX_REASON, t('reason_limit')),
    })
    .superRefine((v, ctx) => {
      if (editing && v.reason.length === 0)
        ctx.addIssue({ code: 'custom', path: ['reason'], message: t('reason_required') });
      const account = accounts.find((a) => a.id === v.accountId);
      if (account?.visibility === 'Household' && v.fundingSource === 'Individual') {
        if (!v.paidByPersonId)
          ctx.addIssue({ code: 'custom', path: ['paidByPersonId'], message: t('payer_required') });
        if (v.participantIds.length === 0)
          ctx.addIssue({
            code: 'custom',
            path: ['participantIds'],
            message: t('participants_required'),
          });
      }
    });

  const {
    register,
    handleSubmit,
    control,
    setValue,
    getValues,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      amount: expense?.amount ?? '',
      occurredOn: expense?.occurredOn ?? todayLocal(),
      category: (expense?.category as ExpenseCategory | undefined) ?? 'Groceries',
      accountId: initialAccount,
      fundingSource: (expense?.fundingSource as FundingSource | undefined) ?? 'Individual',
      paidByPersonId:
        expense?.paidByPersonId ??
        (myPersonId && adultIds.includes(myPersonId) ? myPersonId : (adultIds[0] ?? '')),
      participantIds: expense ? expense.shares.map((s) => s.personId) : adultIds,
      reason: '',
    },
  });
  const values = useWatch({ control });
  const account = accounts.find((a) => a.id === values.accountId);
  const personal = account?.visibility === 'Personal';
  const shared = !personal;
  const split = shared && values.fundingSource === 'Individual';
  const participantIds = values.participantIds ?? [];
  const ownerName = personal
    ? account.ownerPersonId === myPersonId
      ? t('you')
      : (members.find((m) => m.personId === account.ownerPersonId)?.displayName ?? '')
    : '';
  const currency = budget.data?.currency ?? '';

  // Possible duplicates come only from the authorised list, never from a separate check.
  const amount = normalizeAmount(values.amount ?? '');
  const date = values.occurredOn ?? '';
  const hintFilters =
    amount && isIsoDate(date)
      ? {
          amount,
          category: values.category ?? 'Groceries',
          from: shiftDays(date, -2),
          to: shiftDays(date, 2),
          pageSize: 5,
          ...(expense ? { excludeId: expense.id } : {}),
        }
      : null;
  const hint = useExpensesQuery(hintFilters ?? {}, hintFilters !== null);
  const hintKey = JSON.stringify(hintFilters);
  const matches = hintFilters && hint.isSuccess ? hint.data.items : [];
  const [decision, setDecision] = useState<{ key: string; kind: 'keep' | 'skip' } | null>(null);
  const decided = decision?.key === hintKey ? decision.kind : null;
  const checking = hintFilters !== null && hint.isPending;
  const lookupFailed = hintFilters !== null && hint.isError;
  const blocked =
    checking || (matches.length > 0 && decided !== 'keep') || (lookupFailed && decided !== 'skip');

  const accountName = (id: string) => accountsQuery.data?.find((a) => a.id === id)?.name ?? '';
  const toggleParticipant = (id: string) => {
    const current = getValues('participantIds');
    setValue(
      'participantIds',
      current.includes(id) ? current.filter((p) => p !== id) : [...current, id],
      { shouldValidate: true },
    );
  };

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent
        className="max-h-[90dvh] overflow-y-auto"
        aria-describedby="expense-description"
      >
        <DialogTitle>{editing ? t('correct_expense') : t('add_expense')}</DialogTitle>
        <DialogDescription id="expense-description">
          {editing ? t('correct_description') : t('add_description')}
        </DialogDescription>
        <form
          className="space-y-4"
          noValidate
          onSubmit={(event) => {
            void handleSubmit((data) => {
              if (blocked) return;
              const input: ExpenseInput = {
                accountId: data.accountId,
                amount: normalizeAmount(data.amount) ?? data.amount,
                occurredOn: data.occurredOn,
                category: data.category,
                fundingSource: personal ? 'Individual' : data.fundingSource,
                paidByPersonId: split ? data.paidByPersonId : null,
                participantIds: split ? data.participantIds : [],
              };
              const reason = data.reason.trim();
              const key = requestId.idFor(
                JSON.stringify([input, reason, expense?.id, expense?.revision]),
              );
              mutation.mutate(
                expense
                  ? {
                      kind: 'update',
                      requestId: key,
                      id: expense.id,
                      expectedRevision: Number(expense.revision),
                      reason,
                      input,
                    }
                  : { kind: 'create', requestId: key, input },
                {
                  onSuccess: () => {
                    requestId.reset();
                    toast.success(t(editing ? 'expense_corrected' : 'expense_added'));
                    onClose();
                  },
                },
              );
            })(event);
          }}
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              id="expense-amount"
              label={t('amount', { currency })}
              error={errors.amount?.message}
            >
              <Input
                id="expense-amount"
                inputMode="decimal"
                autoComplete="off"
                {...register('amount')}
                aria-invalid={!!errors.amount}
              />
            </Field>
            <Field
              id="expense-date"
              label={t('date')}
              error={errors.occurredOn?.message}
              hint={isIsoDate(date) && date > todayLocal() ? t('date_future') : undefined}
            >
              <Input
                id="expense-date"
                type="date"
                {...register('occurredOn')}
                aria-invalid={!!errors.occurredOn}
              />
            </Field>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field id="expense-category" label={t('category')}>
              <Select id="expense-category" {...register('category')}>
                {EXPENSE_CATEGORIES.map((c) => (
                  <option key={c} value={c}>
                    {t(`categories.${c}`)}
                  </option>
                ))}
              </Select>
            </Field>
            <Field id="expense-envelope" label={t('envelope')} error={errors.accountId?.message}>
              <Select id="expense-envelope" disabled={editing} {...register('accountId')}>
                {accounts.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.name}
                  </option>
                ))}
              </Select>
            </Field>
          </div>

          <p className="text-text-2 text-sm">
            {t('recorded_by_you', {
              name: members.find((m) => m.personId === myPersonId)?.displayName ?? '',
            })}
          </p>

          {personal && (
            <p className="text-text-2 text-sm">{t('paid_by_owner', { name: ownerName })}</p>
          )}

          {shared && (
            <Field id="expense-funding" label={t('funding')}>
              <Select id="expense-funding" {...register('fundingSource')}>
                <option value="Individual">{t('funding_individual')}</option>
                <option value="HouseholdFunds">{t('funding_household')}</option>
              </Select>
            </Field>
          )}
          {shared && values.fundingSource === 'HouseholdFunds' && (
            <p className="text-text-2 text-sm">{t('household_funds_note')}</p>
          )}

          {split && (
            <>
              <Field id="expense-payer" label={t('paid_by')} error={errors.paidByPersonId?.message}>
                <Select id="expense-payer" {...register('paidByPersonId')}>
                  {people.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.former ? t('former', { name: p.name }) : p.name}
                    </option>
                  ))}
                </Select>
              </Field>
              <fieldset className="space-y-2">
                <legend className="text-text-2 mb-1 text-xs font-semibold">
                  {t('split_between')}
                </legend>
                {people.map((p) => (
                  <label key={p.id} className="flex items-center gap-2 text-sm">
                    <Checkbox
                      checked={participantIds.includes(p.id)}
                      onChange={() => {
                        toggleParticipant(p.id);
                      }}
                    />
                    {p.former ? t('former', { name: p.name }) : p.name}
                  </label>
                ))}
                <p className="text-muted-foreground text-xs">{t('split_equal_note')}</p>
                {errors.participantIds?.message && (
                  <p className="text-destructive text-xs">{errors.participantIds.message}</p>
                )}
              </fieldset>
            </>
          )}

          {editing && (
            <Field
              id="expense-reason"
              label={t('reason')}
              error={errors.reason?.message}
              hint={t('reason_hint')}
            >
              <Input
                id="expense-reason"
                maxLength={MAX_REASON}
                {...register('reason')}
                aria-invalid={!!errors.reason}
              />
            </Field>
          )}

          <div role="status" aria-live="polite" className="space-y-2">
            {checking && <p className="text-text-2 text-sm">{t('duplicates_checking')}</p>}
            {matches.length > 0 && (
              <div className="border-border space-y-2 rounded-xl border p-3">
                <p className="text-sm font-semibold">
                  {t('duplicates_title', { count: matches.length })}
                </p>
                <ul className="space-y-1 text-sm">
                  {matches.map((m) => (
                    <li key={m.id}>
                      {m.occurredOn} · {t(`categories.${m.category}`)} · {m.amount} {currency} ·{' '}
                      {accountName(m.accountId)} · {m.addedByDisplayName}
                    </li>
                  ))}
                </ul>
                {decided === 'keep' ? (
                  <p className="text-text-2 text-sm">{t('duplicates_kept')}</p>
                ) : (
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      size="sm"
                      onClick={() => {
                        setDecision({ key: hintKey, kind: 'keep' });
                      }}
                    >
                      {t('keep_both')}
                    </Button>
                    <Button type="button" size="sm" variant="outline" onClick={onClose}>
                      {t('cancel')}
                    </Button>
                  </div>
                )}
              </div>
            )}
            {lookupFailed && (
              <Banner variant="warning">
                <p>{t('duplicates_failed')}</p>
                {decided === 'skip' ? (
                  <p className="mt-1">{t('duplicates_skipped')}</p>
                ) : (
                  <div className="mt-2 flex flex-wrap gap-2">
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      onClick={() => {
                        void hint.refetch();
                      }}
                    >
                      {t('retry')}
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      onClick={() => {
                        setDecision({ key: hintKey, kind: 'skip' });
                      }}
                    >
                      {t('save_without_checking')}
                    </Button>
                  </div>
                )}
              </Banner>
            )}
          </div>

          {mutation.isError &&
            (errorStatus(mutation.error) === 409 && editing ? (
              <Banner
                variant="error"
                {...(onReload ? { onRetry: onReload, retryLabel: t('reload') } : {})}
              >
                {t('expense_conflict')}
              </Banner>
            ) : (
              <Banner variant="error">
                {errorStatus(mutation.error) === 422
                  ? t('expense_rejected')
                  : t('expense_save_error')}
              </Banner>
            ))}

          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" onClick={onClose}>
              {t('cancel')}
            </Button>
            <Button type="submit" disabled={mutation.isPending || blocked}>
              {mutation.isPending ? t('saving') : t('save')}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
