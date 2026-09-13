import { zodResolver } from '@hookform/resolvers/zod';
import { Banner, Button, Field, Input } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

import { useHouseholdMutation } from '../api/queries';

interface HouseholdNameFormProps {
  household?: { id: string; name: string };
}

export function HouseholdNameForm({ household }: HouseholdNameFormProps) {
  const { t } = useTranslation('household');
  const mutation = useHouseholdMutation();
  const toast = useToast();
  const schema = z.object({
    name: z.string().trim().min(1, t('required')).max(120, t('name_limit')),
  });
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: { name: household?.name ?? t('suggested_name') },
  });
  return (
    <form
      className="space-y-4"
      noValidate
      onSubmit={(event) => {
        void handleSubmit((data) => {
          mutation.mutate(
            household ? { kind: 'rename', id: household.id, ...data } : { kind: 'create', ...data },
            {
              onSuccess: () => {
                if (household) toast.success(t('saved'));
              },
            },
          );
        })(event);
      }}
    >
      <Field
        id="household-name"
        label={t('name')}
        error={errors.name?.message}
        hint={household ? undefined : t('rename_later')}
      >
        <Input
          id="household-name"
          {...register('name')}
          maxLength={120}
          aria-invalid={!!errors.name}
        />
      </Field>
      {mutation.isError && <Banner variant="error">{t('save_error')}</Banner>}
      <Button type="submit" disabled={mutation.isPending}>
        {t(mutation.isPending ? 'saving' : household ? 'save_name' : 'start')}
      </Button>
    </form>
  );
}
