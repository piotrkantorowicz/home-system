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
import { useExpenseMutation } from '../api/queries';
import { useRequestId } from '../hooks/useRequestId';

import type { Expense } from '../types';

interface VoidExpenseDialogProps {
  expense: Expense;
  onClose: () => void;
  onReload: () => void;
}

export function VoidExpenseDialog({ expense, onClose, onReload }: VoidExpenseDialogProps) {
  const { t } = useTranslation('budget');
  const toast = useToast();
  const mutation = useExpenseMutation();
  const requestId = useRequestId();
  const [reason, setReason] = useState('');
  const [touched, setTouched] = useState(false);
  const trimmed = reason.trim();
  const invalid = trimmed.length === 0;
  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent aria-describedby="void-description">
        <DialogTitle>{t('void_expense')}</DialogTitle>
        <DialogDescription id="void-description">{t('void_description')}</DialogDescription>
        <form
          className="space-y-4"
          noValidate
          onSubmit={(event) => {
            event.preventDefault();
            setTouched(true);
            if (invalid) return;
            mutation.mutate(
              {
                kind: 'void',
                requestId: requestId.idFor(JSON.stringify([expense.id, expense.revision, trimmed])),
                id: expense.id,
                expectedRevision: Number(expense.revision),
                reason: trimmed,
              },
              {
                onSuccess: () => {
                  requestId.reset();
                  toast.success(t('expense_voided'));
                  onClose();
                },
              },
            );
          }}
        >
          <Field
            id="void-reason"
            label={t('reason')}
            hint={t('reason_hint')}
            error={touched && invalid ? t('reason_required') : undefined}
          >
            <Input
              id="void-reason"
              value={reason}
              maxLength={200}
              aria-invalid={touched && invalid}
              onChange={(event) => {
                setReason(event.target.value);
              }}
            />
          </Field>
          {mutation.isError &&
            (errorStatus(mutation.error) === 409 ? (
              <Banner variant="error" onRetry={onReload} retryLabel={t('reload')}>
                {t('expense_conflict')}
              </Banner>
            ) : (
              <Banner variant="error">{t('expense_save_error')}</Banner>
            ))}
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" onClick={onClose}>
              {t('cancel')}
            </Button>
            <Button type="submit" variant="destructive" disabled={mutation.isPending}>
              {mutation.isPending ? t('saving') : t('void_confirm')}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
