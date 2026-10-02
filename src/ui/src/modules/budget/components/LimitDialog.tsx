import {
  Banner,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  Field,
  Input,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { errorStatus } from '../api/client';
import { useLimitMutation } from '../api/queries';
import { normalizeLimit } from '../lib/money';

interface LimitDialogProps {
  accountId: string;
  accountName: string;
  month: string;
  currency: string;
  /** The current limit, when one is set. */
  limit: { amount: string; revision: number } | null;
  onClose: () => void;
  /** A conflict: reload the latest summary (the caller refetches and closes this dialog). */
  onReload: () => void;
}

export function LimitDialog({
  accountId,
  accountName,
  month,
  currency,
  limit,
  onClose,
  onReload,
}: LimitDialogProps) {
  const { t } = useTranslation('budget');
  const toast = useToast();
  const mutation = useLimitMutation();
  const [amount, setAmount] = useState(limit?.amount ?? '');
  const [touched, setTouched] = useState(false);
  const normalized = normalizeLimit(amount);
  const invalid = touched && normalized === null;
  const done = (message: string) => ({
    onSuccess: () => {
      toast.success(t(message));
      onClose();
    },
  });
  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent aria-describedby="limit-description">
        <DialogTitle>{t('limit_title', { name: accountName, month })}</DialogTitle>
        <DialogDescription id="limit-description">{t('limit_description')}</DialogDescription>
        <form
          className="space-y-4"
          noValidate
          onSubmit={(event) => {
            event.preventDefault();
            setTouched(true);
            if (normalized === null) return;
            mutation.mutate(
              {
                kind: 'set',
                accountId,
                month,
                amount: normalized,
                expectedRevision: limit?.revision ?? null,
              },
              done('limit_saved'),
            );
          }}
        >
          <Field
            id="limit-amount"
            label={t('limit_amount', { currency })}
            hint={t('limit_hint')}
            error={invalid ? t('limit_invalid') : undefined}
          >
            <Input
              id="limit-amount"
              inputMode="decimal"
              autoComplete="off"
              value={amount}
              aria-invalid={invalid}
              onChange={(event) => {
                setAmount(event.target.value);
              }}
            />
          </Field>
          {mutation.isError &&
            (errorStatus(mutation.error) === 409 ? (
              <Banner variant="error" onRetry={onReload} retryLabel={t('reload')}>
                {t('limit_conflict')}
              </Banner>
            ) : (
              <Banner variant="error">{t('limit_error')}</Banner>
            ))}
          <div className="flex flex-wrap justify-between gap-2">
            {limit ? (
              <Button
                type="button"
                variant="ghost"
                disabled={mutation.isPending}
                onClick={() => {
                  mutation.mutate(
                    { kind: 'clear', accountId, month, expectedRevision: limit.revision },
                    done('limit_cleared'),
                  );
                }}
              >
                {t('limit_clear')}
              </Button>
            ) : (
              <span />
            )}
            <div className="flex gap-2">
              <Button type="button" variant="ghost" onClick={onClose}>
                {t('cancel')}
              </Button>
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? t('saving') : t('save')}
              </Button>
            </div>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
