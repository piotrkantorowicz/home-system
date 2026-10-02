import { zodResolver } from '@hookform/resolvers/zod';
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
import { useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

import { errorStatus } from '../api/client';
import { useBudgetMutation } from '../api/queries';
import { useBudgetAccess } from '../hooks/useBudgetAccess';

import type { Account } from '../types';

interface EnvelopeFormDialogProps {
  /** Present when renaming; absent when creating. */
  account?: Account;
  onClose: () => void;
}

export function EnvelopeFormDialog({ account, onClose }: EnvelopeFormDialogProps) {
  const { t } = useTranslation('budget');
  const toast = useToast();
  const mutation = useBudgetMutation();
  const { level } = useBudgetAccess();
  const { members, myPersonId } = useHousehold();
  const adult = level === 'adult';
  // Adults may open a personal envelope for themselves or someone they manage; never for another adult.
  const owners = members.filter((m) => m.personId === myPersonId || (adult && m.isManaged));
  const schema = z.object({
    name: z.string().trim().min(1, t('name_required')).max(80, t('name_limit')),
    visibility: z.enum(['Household', 'Personal']),
    ownerPersonId: z.string(),
  });
  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      name: account?.name ?? '',
      visibility: adult ? ('Household' as const) : ('Personal' as const),
      ownerPersonId: myPersonId ?? '',
    },
  });
  const visibility = useWatch({ control, name: 'visibility' });
  const renaming = account !== undefined;
  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent aria-describedby="envelope-description">
        <DialogTitle>{renaming ? t('rename_envelope') : t('new_envelope')}</DialogTitle>
        <DialogDescription id="envelope-description">
          {renaming ? t('rename_description') : t('new_description')}
        </DialogDescription>
        <form
          className="space-y-4"
          noValidate
          onSubmit={(event) => {
            void handleSubmit((data) => {
              mutation.mutate(
                account
                  ? {
                      kind: 'rename',
                      id: account.id,
                      name: data.name,
                      expectedRevision: Number(account.revision),
                    }
                  : {
                      kind: 'create',
                      name: data.name,
                      visibility: data.visibility,
                      ownerPersonId: data.visibility === 'Personal' ? data.ownerPersonId : null,
                    },
                {
                  onSuccess: () => {
                    toast.success(t(renaming ? 'renamed' : 'created'));
                    onClose();
                  },
                },
              );
            })(event);
          }}
        >
          <Field id="envelope-name" label={t('name')} error={errors.name?.message}>
            <Input
              id="envelope-name"
              {...register('name')}
              maxLength={80}
              aria-invalid={!!errors.name}
            />
          </Field>
          {!renaming && adult && (
            <Field id="envelope-audience" label={t('audience')}>
              <Select id="envelope-audience" {...register('visibility')}>
                <option value="Household">{t('audience_shared')}</option>
                <option value="Personal">{t('audience_personal')}</option>
              </Select>
            </Field>
          )}
          {!renaming && visibility === 'Personal' && adult && (
            <Field id="envelope-owner" label={t('owner')}>
              <Select id="envelope-owner" {...register('ownerPersonId')}>
                {owners.map((m) => (
                  <option key={m.personId} value={m.personId}>
                    {m.personId === myPersonId ? t('you') : m.displayName}
                  </option>
                ))}
              </Select>
            </Field>
          )}
          {!renaming && (
            <p className="text-text-2 text-sm">
              {visibility === 'Personal' ? t('privacy_personal') : t('privacy_shared')}
            </p>
          )}
          {mutation.isError && (
            <Banner variant="error">
              {errorStatus(mutation.error) === 409 ? t('conflict_error') : t('save_error')}
            </Banner>
          )}
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" onClick={onClose}>
              {t('cancel')}
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? t('saving') : t('save')}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
