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
import { useRepaymentMutation } from '../api/queries';

import type { Repayment } from '../types';

interface VoidRepaymentDialogProps {
  repayment: Repayment;
  onClose: () => void;
}

export function VoidRepaymentDialog({ repayment, onClose }: VoidRepaymentDialogProps) {
  const { t } = useTranslation('budget');
  const toast = useToast();
  const mutation = useRepaymentMutation();
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
      <DialogContent aria-describedby="void-repayment-description">
        <DialogTitle>{t('void_repayment')}</DialogTitle>
        <DialogDescription id="void-repayment-description">
          {t('void_repayment_description')}
        </DialogDescription>
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
                id: repayment.id,
                expectedRevision: Number(repayment.revision),
                reason: trimmed,
              },
              {
                onSuccess: () => {
                  toast.success(t('repayment_voided'));
                  onClose();
                },
              },
            );
          }}
        >
          <Field
            id="void-repayment-reason"
            label={t('reason')}
            hint={t('reason_hint')}
            error={touched && invalid ? t('reason_required') : undefined}
          >
            <Input
              id="void-repayment-reason"
              value={reason}
              maxLength={200}
              aria-invalid={touched && invalid}
              onChange={(event) => {
                setReason(event.target.value);
              }}
            />
          </Field>
          {mutation.isError && (
            // onSettled already refetched the history, so the row behind this dialog is current.
            <Banner variant="error">
              {errorStatus(mutation.error) === 409
                ? t('repayment_conflict')
                : t('repayment_save_error')}
            </Banner>
          )}
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" onClick={onClose}>
              {t('cancel')}
            </Button>
            <Button type="submit" variant="destructive" disabled={mutation.isPending}>
              {mutation.isPending ? t('saving') : t('void_repayment')}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
