import { zodResolver } from '@hookform/resolvers/zod';
import {
  Banner,
  Button,
  Dialog,
  DialogContent,
  DialogTitle,
  DialogDescription,
  Field,
  Input,
  Select,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

import { useHouseholdMutation, usePickablePersons } from '../api/queries';
import { HOUSEHOLD_ROLES } from '../types';

type AddMode = 'existing' | 'invite' | 'managed';
interface AddMemberDialogProps {
  id: string;
  onClose: () => void;
}

export function AddMemberDialog({ id, onClose }: AddMemberDialogProps) {
  const { t } = useTranslation('household');
  const [mode, setMode] = useState<AddMode>('existing');
  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent className="max-h-[90dvh] overflow-y-auto" aria-describedby="add-description">
        <DialogTitle>{t('add_member')}</DialogTitle>
        <DialogDescription id="add-description">{t('add_description')}</DialogDescription>
        <div role="tablist" aria-label={t('add_method')} className="flex flex-wrap gap-2">
          {(['existing', 'invite', 'managed'] as const).map((value, index, modes) => (
            <Button
              key={value}
              type="button"
              role="tab"
              id={`tab-${value}`}
              aria-controls={`panel-${value}`}
              aria-selected={mode === value}
              tabIndex={mode === value ? 0 : -1}
              variant={mode === value ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => {
                setMode(value);
              }}
              onKeyDown={(event) => {
                const next =
                  event.key === 'ArrowRight'
                    ? modes[(index + 1) % modes.length]
                    : event.key === 'ArrowLeft'
                      ? modes[(index + modes.length - 1) % modes.length]
                      : event.key === 'Home'
                        ? modes[0]
                        : event.key === 'End'
                          ? modes[2]
                          : undefined;
                if (next) {
                  event.preventDefault();
                  setMode(next);
                  document.getElementById(`tab-${next}`)?.focus();
                }
              }}
            >
              {t(value)}
            </Button>
          ))}
        </div>
        <div role="tabpanel" id={`panel-${mode}`} aria-labelledby={`tab-${mode}`}>
          <AddMemberForm key={mode} mode={mode} id={id} onClose={onClose} />
        </div>
      </DialogContent>
    </Dialog>
  );
}

function AddMemberForm({ mode, id, onClose }: AddMemberDialogProps & { mode: AddMode }) {
  const { t } = useTranslation('household');
  const toast = useToast();
  const persons = usePickablePersons(mode === 'existing');
  const mutation = useHouseholdMutation();
  const schema = z.object({
    value:
      mode === 'invite'
        ? z.email(t('email_invalid'))
        : z.string().trim().min(1, t('required')).max(200, t('name_limit')),
    role: z.enum(['Owner', 'Adult', 'Child', 'Guest']),
  });
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      value: '',
      role: mode === 'managed' ? ('Child' as const) : ('Adult' as const),
    },
  });
  return (
    <form
      className="space-y-4"
      noValidate
      onSubmit={(event) => {
        void handleSubmit(({ value, role }) => {
          const action =
            mode === 'existing'
              ? { kind: mode, id, personId: value, role }
              : mode === 'managed'
                ? { kind: mode, id, displayName: value, role }
                : { kind: mode, id, email: value, role };
          mutation.mutate(action, {
            onSuccess: (result) => {
              const pending =
                mode === 'invite' &&
                result &&
                'addedImmediately' in result &&
                !result.addedImmediately;
              toast.success(t(pending ? 'invitation_created' : 'member_added'));
              onClose();
            },
          });
        })(event);
      }}
    >
      <p className="text-text-2 text-sm">{t(`${mode}_hint`)}</p>
      {mode === 'existing' && persons.isError && (
        <Banner
          variant="error"
          onRetry={() => {
            void persons.refetch();
          }}
          retryLabel={t('retry')}
        >
          {t('people_error')}
        </Banner>
      )}
      <Field
        id="member-value"
        label={t(mode === 'existing' ? 'person' : mode === 'invite' ? 'email' : 'display_name')}
        error={errors.value?.message}
      >
        {mode === 'existing' ? (
          <Select
            id="member-value"
            {...register('value')}
            aria-invalid={!!errors.value}
            disabled={persons.isPending || persons.isError}
          >
            <option value="">
              {t(
                persons.isPending
                  ? 'loading'
                  : persons.data?.length
                    ? 'select_person'
                    : 'no_people',
              )}
            </option>
            {persons.data?.map((person) => (
              <option key={person.personId} value={person.personId}>
                {person.displayName}
                {person.email ? ` (${person.email})` : ''}
              </option>
            ))}
          </Select>
        ) : (
          <Input
            id="member-value"
            {...register('value')}
            type={mode === 'invite' ? 'email' : 'text'}
            aria-invalid={!!errors.value}
          />
        )}
      </Field>
      <Field id="member-role" label={t('role')}>
        <Select id="member-role" {...register('role')}>
          {HOUSEHOLD_ROLES.map((role) => (
            <option key={role} value={role}>
              {t(`roles.${role}`)}
            </option>
          ))}
        </Select>
      </Field>
      <p className="text-muted-foreground text-xs">{t('role_hint')}</p>
      {mutation.isError && <Banner variant="error">{t('save_error')}</Banner>}
      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onClose}>
          {t('cancel')}
        </Button>
        <Button
          type="submit"
          disabled={mutation.isPending || (mode === 'existing' && !persons.data?.length)}
        >
          {t(mutation.isPending ? 'saving' : mode === 'invite' ? 'invite_action' : 'add_member')}
        </Button>
      </div>
    </form>
  );
}
