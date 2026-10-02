import { zodResolver } from '@hookform/resolvers/zod';
import { Banner, Button, Field, Select } from '@shared/components/ui';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

import { useBudgetMutation } from '../api/queries';
import { BUDGET_CURRENCIES } from '../types';

const schema = z.object({ currency: z.enum(BUDGET_CURRENCIES) });

export function BudgetSetup() {
  const { t } = useTranslation('budget');
  const mutation = useBudgetMutation();
  const { register, handleSubmit } = useForm({
    resolver: zodResolver(schema),
    defaultValues: { currency: 'PLN' as const },
  });
  return (
    <form
      className="max-w-md space-y-4"
      noValidate
      onSubmit={(event) => {
        void handleSubmit((data) => {
          mutation.mutate({ kind: 'initialize', currency: data.currency });
        })(event);
      }}
    >
      <p className="text-text-2 text-sm">{t('setup_intro')}</p>
      <Field id="budget-currency" label={t('currency')} hint={t('currency_hint')}>
        <Select id="budget-currency" {...register('currency')}>
          {BUDGET_CURRENCIES.map((code) => (
            <option key={code} value={code}>
              {code}
            </option>
          ))}
        </Select>
      </Field>
      {mutation.isError && <Banner variant="error">{t('setup_error')}</Banner>}
      <Button type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? t('saving') : t('setup_submit')}
      </Button>
    </form>
  );
}
