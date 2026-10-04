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
import { initials } from '@shared/lib/initials';
import { cn } from '@shared/lib/utils';
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
import { minorToDecimal, normalizeAmount, splitEqual, toMinor } from '../lib/money';
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
const MAX_DESCRIPTION = 80;
const HOUSEHOLD_PAYER = 'household';

function ExpenseFormDialogContent({ expense, onClose, onReload }: ExpenseFormDialogProps) {
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
      description: z.string().trim().max(MAX_DESCRIPTION, t('what_for_limit')),
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
      description: expense?.description ?? '',
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

  const amountMinor = amount === null ? null : toMinor(amount);
  const shares =
    amountMinor !== null && split
      ? splitEqual(amountMinor, participantIds)
      : new Map<string, number>();
  const payer = values.fundingSource === 'HouseholdFunds' ? HOUSEHOLD_PAYER : values.paidByPersonId;
  const choosePayer = (choice: string) => {
    if (choice === HOUSEHOLD_PAYER) {
      setValue('fundingSource', 'HouseholdFunds', { shouldValidate: true });
      return;
    }
    setValue('fundingSource', 'Individual', { shouldValidate: true });
    setValue('paidByPersonId', choice, { shouldValidate: true });
  };
  const personLabel = (p: Person) =>
    p.former ? t('former', { name: p.name }) : p.id === myPersonId ? t('you') : p.name;
  const saveLabel = mutation.isPending
    ? t('saving')
    : amount !== null
      ? t('save_amount', { amount: minorToDecimal(toMinor(amount)) })
      : t('save');
  const chip =
    'border-border bg-card text-text-2 hover:border-border-strong has-checked:border-primary has-checked:bg-accent has-checked:text-primary has-focus-visible:ring-ring flex min-h-11 cursor-pointer items-center gap-2 rounded-xl border px-3.5 text-sm font-semibold has-focus-visible:ring-2';

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent
        className="flex max-h-[90dvh] flex-col gap-0 overflow-hidden p-0 max-md:inset-0 max-md:top-0 max-md:left-0 max-md:h-dvh max-md:max-h-none max-md:max-w-none max-md:translate-x-0 max-md:translate-y-0 max-md:rounded-none max-md:border-0 md:max-w-[560px]"
        aria-describedby="expense-description"
      >
        <div className="shrink-0 px-5 pt-5 pb-3 md:px-6 md:pt-6">
          <DialogTitle>{editing ? t('correct_expense') : t('add_expense')}</DialogTitle>
          <DialogDescription id="expense-description" className="sr-only">
            {editing ? t('correct_description') : t('add_description')}
          </DialogDescription>
        </div>
        <form
          className="flex min-h-0 flex-1 flex-col"
          noValidate
          onSubmit={(event) => {
            void handleSubmit((data) => {
              if (blocked) return;
              const note = data.description.trim();
              const input: ExpenseInput = {
                accountId: data.accountId,
                amount: normalizeAmount(data.amount) ?? data.amount,
                occurredOn: data.occurredOn,
                category: data.category,
                fundingSource: personal ? 'Individual' : data.fundingSource,
                paidByPersonId: split ? data.paidByPersonId : null,
                participantIds: split ? data.participantIds : [],
                description: note.length > 0 ? note : null,
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
          <div className="min-h-0 flex-1 space-y-5 overflow-y-auto px-5 py-2 md:px-6">
            <Field
              id="expense-amount"
              label={t('amount_label')}
              error={errors.amount?.message}
              className="text-center md:text-left"
            >
              <div className="relative">
                <Input
                  id="expense-amount"
                  aria-label={t('amount', { currency })}
                  inputMode="decimal"
                  autoComplete="off"
                  // REASON: the amount is the one thing this form is for (SPEC §5.3 asks for autofocus).
                  // eslint-disable-next-line jsx-a11y/no-autofocus
                  autoFocus
                  placeholder="0.00"
                  className="h-16 pr-16 text-[36px] font-semibold md:pl-5"
                  {...register('amount')}
                  aria-invalid={!!errors.amount}
                />
                <span
                  aria-hidden="true"
                  className="text-text-2 pointer-events-none absolute top-1/2 right-4 -translate-y-1/2 text-lg"
                >
                  {currency}
                </span>
              </div>
            </Field>

            <Field
              id="expense-note"
              label={t('what_for')}
              hint={t('what_for_optional')}
              error={errors.description?.message}
            >
              <Input
                id="expense-note"
                maxLength={MAX_DESCRIPTION}
                autoComplete="off"
                {...register('description')}
                aria-invalid={!!errors.description}
              />
            </Field>

            <fieldset>
              <legend className="text-label text-text-2 mb-1.5 font-semibold">
                {t('category')}
              </legend>
              <div className="flex flex-wrap gap-2">
                {EXPENSE_CATEGORIES.map((c) => (
                  <label key={c} className={chip}>
                    <input type="radio" value={c} className="sr-only" {...register('category')} />
                    {t(`categories.${c}`)}
                  </label>
                ))}
              </div>
            </fieldset>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field id="expense-envelope" label={t('envelope')} error={errors.accountId?.message}>
                <Select id="expense-envelope" disabled={editing} {...register('accountId')}>
                  {accounts.map((a) => (
                    <option key={a.id} value={a.id}>
                      {t(a.visibility === 'Personal' ? 'envelope_personal' : 'envelope_shared', {
                        name: a.name,
                      })}
                    </option>
                  ))}
                </Select>
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

            {personal && (
              <p className="text-text-2 text-sm">{t('paid_by_owner', { name: ownerName })}</p>
            )}

            {shared && (
              <fieldset>
                <legend className="text-label text-text-2 mb-1.5 font-semibold">
                  {t('who_paid')}
                </legend>
                <div className="grid gap-2 sm:grid-cols-3">
                  {people.map((p) => (
                    <label key={p.id} className={chip}>
                      <input
                        type="radio"
                        name="expense-payer"
                        className="sr-only"
                        checked={payer === p.id}
                        onChange={() => {
                          choosePayer(p.id);
                        }}
                      />
                      <Avatar label={p.name} />
                      <span className="min-w-0 break-words">{personLabel(p)}</span>
                    </label>
                  ))}
                  <label className={chip}>
                    <input
                      type="radio"
                      name="expense-payer"
                      className="sr-only"
                      checked={payer === HOUSEHOLD_PAYER}
                      onChange={() => {
                        choosePayer(HOUSEHOLD_PAYER);
                      }}
                    />
                    <Avatar label={t('household_account_initial')} />
                    <span className="min-w-0 break-words">{t('household_account')}</span>
                  </label>
                </div>
                {errors.paidByPersonId?.message && (
                  <p className="text-destructive mt-1 text-xs">{errors.paidByPersonId.message}</p>
                )}
              </fieldset>
            )}
            {shared && values.fundingSource === 'HouseholdFunds' && (
              <p className="text-text-2 text-sm">{t('household_funds_note')}</p>
            )}

            {split && (
              <fieldset>
                <legend className="text-label text-text-2 mb-1.5 font-semibold">
                  {t('split_between')}
                </legend>
                <div className="grid gap-2 sm:grid-cols-2">
                  {people.map((p) => {
                    const on = participantIds.includes(p.id);
                    const share = shares.get(p.id);
                    return (
                      <label key={p.id} className={cn(chip, 'justify-between')}>
                        <span className="flex min-w-0 items-center gap-2">
                          <Checkbox
                            checked={on}
                            aria-label={p.former ? t('former', { name: p.name }) : p.name}
                            onChange={() => {
                              toggleParticipant(p.id);
                            }}
                          />
                          <span className="min-w-0 break-words">{personLabel(p)}</span>
                        </span>
                        <span className="tnum" aria-hidden="true">
                          {on && share !== undefined ? minorToDecimal(share) : '–'}
                        </span>
                      </label>
                    );
                  })}
                </div>
                <p className="text-muted-foreground mt-1.5 text-xs">{t('split_equal_note')}</p>
                {errors.participantIds?.message && (
                  <p className="text-destructive text-xs">{errors.participantIds.message}</p>
                )}
              </fieldset>
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
          </div>

          {/* Outside the scrolling body, so Save stays reachable on a phone with the keyboard open. */}
          <div className="border-border bg-card flex shrink-0 flex-wrap items-center justify-between gap-3 border-t px-5 py-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] md:px-6">
            <p className="text-text-2 text-sm max-md:hidden">
              {t('recorded_by_you', {
                name: members.find((m) => m.personId === myPersonId)?.displayName ?? '',
              })}
            </p>
            <div className="flex flex-1 gap-2 max-md:w-full md:flex-none">
              <Button
                type="button"
                variant="outline"
                size="xl"
                onClick={onClose}
                className="max-md:hidden"
              >
                {t('cancel')}
              </Button>
              <Button
                type="submit"
                size="xl"
                className="max-md:flex-1"
                disabled={mutation.isPending || blocked}
              >
                {saveLabel}
              </Button>
            </div>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function Avatar({ label }: { label: string }) {
  return (
    <span
      aria-hidden="true"
      className="bg-accent text-accent-foreground grid size-7 shrink-0 place-items-center rounded-full text-xs font-bold"
    >
      {initials(label)}
    </span>
  );
}

/**
 * The form's defaults (envelope, payer) depend on the envelope list, so wait for it — otherwise a
 * form opened before the list arrived would start with no envelope chosen.
 */
export function ExpenseFormDialog(props: ExpenseFormDialogProps) {
  const { t } = useTranslation('budget');
  const accounts = useAccountsQuery();
  if (accounts.isSuccess) return <ExpenseFormDialogContent {...props} />;
  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) props.onClose();
      }}
    >
      <DialogContent aria-describedby="expense-loading">
        <DialogTitle>{props.expense ? t('correct_expense') : t('add_expense')}</DialogTitle>
        <DialogDescription id="expense-loading" className="sr-only">
          {t('add_description')}
        </DialogDescription>
        {accounts.isError ? (
          <Banner
            variant="error"
            onRetry={() => {
              void accounts.refetch();
            }}
            retryLabel={t('retry')}
          >
            {t('load_error')}
          </Banner>
        ) : (
          <p role="status">{t('loading')}</p>
        )}
      </DialogContent>
    </Dialog>
  );
}
