import { zodResolver } from '@hookform/resolvers/zod';
import { Banner, Button, Field, Input } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { cn } from '@shared/lib/utils';
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
    watch,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(schema),
    defaultValues: { name: household?.name ?? t('suggested_name') },
  });
  // An unchanged name is not a change: Save stays disabled until the trimmed value differs.
  const unchanged = household?.name === watch('name').trim();
  return (
    <form
      className={cn(household ? 'flex flex-wrap items-start gap-3' : 'space-y-4')}
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
        className={household ? 'min-w-0 flex-1' : undefined}
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
      {mutation.isError && (
        <Banner variant="error" className="basis-full">
          {t('save_error')}
        </Banner>
      )}
      <Button
        type="submit"
        className={household ? 'mt-7' : undefined}
        disabled={mutation.isPending || unchanged}
      >
        {t(mutation.isPending ? 'saving' : household ? 'save_name' : 'start')}
      </Button>
    </form>
  );
}
