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
import { useWalletMutation } from '../api/queries';
import {
  WALLET_CURRENCIES,
  WALLET_INSTITUTIONS,
  WALLET_KINDS,
  WALLET_VISIBILITIES,
} from '../types';

import type { Wallet } from '../types';

export interface WalletFormDialogProps {
  /** Present when editing; absent when creating. */
  wallet?: Wallet;
  onClose: () => void;
}

const NAME_MAX = 80;

/** Add or edit a wallet. Owner, institution, kind and currency are fixed once the wallet exists. */
export function WalletFormDialog({ wallet, onClose }: WalletFormDialogProps) {
  const { t } = useTranslation('investments');
  const toast = useToast();
  const mutation = useWalletMutation();
  const { members, myPersonId, myRole } = useHousehold();
  const adult = myRole === 'Owner' || myRole === 'Adult';
  const editing = wallet !== undefined;
  // Adults may add a wallet for themselves or a managed member; never in another adult's name.
  const owners = members.filter((m) => m.personId === myPersonId || (adult && m.isManaged));
  const schema = z.object({
    name: z.string().trim().min(1, t('name_required')).max(NAME_MAX, t('name_limit')),
    institution: z.enum(WALLET_INSTITUTIONS),
    kind: z.enum(WALLET_KINDS),
    currency: z.enum(WALLET_CURRENCIES),
    visibility: z.enum(WALLET_VISIBILITIES),
    ownerPersonId: z.string(),
  });
  const {
    register,
    handleSubmit,
    control,
    getValues,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      name: wallet?.name ?? '',
      institution: (wallet?.institution ?? 'XTB') as z.infer<typeof schema>['institution'],
      kind: (wallet?.kind ?? 'Brokerage') as z.infer<typeof schema>['kind'],
      currency: (wallet?.currency ?? 'PLN') as z.infer<typeof schema>['currency'],
      visibility: (wallet?.visibility ?? (adult ? 'Shared' : 'Private')) as z.infer<
        typeof schema
      >['visibility'],
      ownerPersonId: wallet?.ownerPersonId ?? myPersonId ?? '',
    },
  });
  const visibility = useWatch({ control, name: 'visibility' });
  // A disabled field registered with react-hook-form submits as undefined, so fixed fields are
  // shown unregistered; their values stay in the form from defaultValues.
  const fixed = (name: keyof z.infer<typeof schema>) =>
    editing ? { disabled: true, defaultValue: getValues(name) } : register(name);
  const ownerName = (personId: string, fallback: string) =>
    personId === myPersonId
      ? t('you')
      : (members.find((m) => m.personId === personId)?.displayName ?? fallback);

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent aria-describedby="wallet-description">
        <DialogTitle>{editing ? t('edit_wallet') : t('add_wallet')}</DialogTitle>
        <DialogDescription id="wallet-description">
          {editing ? t('edit_wallet_description') : t('add_wallet_description')}
        </DialogDescription>
        <form
          className="space-y-4"
          noValidate
          onSubmit={(event) => {
            void handleSubmit((data) => {
              mutation.mutate(
                wallet
                  ? {
                      kind: 'update',
                      id: wallet.id,
                      name: data.name,
                      visibility: data.visibility,
                      version: Number(wallet.version),
                    }
                  : { kind: 'create', wallet: data },
                {
                  onSuccess: () => {
                    toast.success(t(editing ? 'wallet_saved' : 'wallet_created'));
                    onClose();
                  },
                },
              );
            })(event);
          }}
        >
          <Field id="wallet-name" label={t('name')} error={errors.name?.message}>
            <Input
              id="wallet-name"
              {...register('name')}
              maxLength={NAME_MAX}
              aria-invalid={!!errors.name}
            />
          </Field>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field id="wallet-institution" label={t('institution')}>
              <Select id="wallet-institution" {...fixed('institution')}>
                {WALLET_INSTITUTIONS.map((value) => (
                  <option key={value} value={value}>
                    {t(`institution_${value}`)}
                  </option>
                ))}
              </Select>
            </Field>
            <Field id="wallet-kind" label={t('kind')}>
              <Select id="wallet-kind" {...fixed('kind')}>
                {WALLET_KINDS.map((value) => (
                  <option key={value} value={value}>
                    {t(`kind_${value}`)}
                  </option>
                ))}
              </Select>
            </Field>
            <Field id="wallet-currency" label={t('currency')}>
              <Select id="wallet-currency" {...fixed('currency')}>
                {WALLET_CURRENCIES.map((value) => (
                  <option key={value} value={value}>
                    {value}
                  </option>
                ))}
              </Select>
            </Field>
            <Field id="wallet-owner" label={t('owner')}>
              <Select id="wallet-owner" {...fixed('ownerPersonId')}>
                {wallet ? (
                  <option value={wallet.ownerPersonId}>
                    {ownerName(wallet.ownerPersonId, wallet.ownerName)}
                  </option>
                ) : (
                  owners.map((m) => (
                    <option key={m.personId} value={m.personId}>
                      {ownerName(m.personId, m.displayName)}
                    </option>
                  ))
                )}
              </Select>
            </Field>
          </div>
          <Field id="wallet-visibility" label={t('wallet_visibility')}>
            <Select
              id="wallet-visibility"
              {...(wallet && !wallet.canChangeVisibility
                ? fixed('visibility')
                : register('visibility'))}
            >
              {WALLET_VISIBILITIES.map((value) => (
                <option key={value} value={value}>
                  {t(`visibility_${value}`)}
                </option>
              ))}
            </Select>
          </Field>
          <p className="text-text-2 text-sm">
            {visibility === 'Private' ? t('privacy_private') : t('privacy_shared')}
          </p>
          {editing && <p className="text-muted-foreground text-meta">{t('fixed_fields_note')}</p>}
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
