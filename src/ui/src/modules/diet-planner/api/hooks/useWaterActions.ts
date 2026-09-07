import { useToast } from '@shared/context/ToastContext';
import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useDeleteWaterIntake, useLogWaterIntake } from './useHydration';

export function useWaterActions(date: string) {
  const { t } = useTranslation();
  const toast = useToast();
  const log = useLogWaterIntake();
  const deletion = useDeleteWaterIntake();
  const active = useRef(new Set<string>());
  const [pendingKeys, setPendingKeys] = useState(new Set<string>());

  async function run(key: string, action: () => Promise<void>, errorKey: string): Promise<boolean> {
    if (active.current.has(key)) return false;
    active.current.add(key);
    setPendingKeys(new Set(active.current));
    try {
      await action();
      return true;
    } catch {
      toast.error(t(errorKey));
      return false;
    } finally {
      active.current.delete(key);
      setPendingKeys(new Set(active.current));
    }
  }

  function remove(id: string): Promise<boolean> {
    return run(
      `remove-${id}`,
      async () => {
        await deletion.mutateAsync({ id, date });
        toast.success(t('hydration.delete_success'));
      },
      'hydration.delete_error',
    );
  }

  function add(amountMl: number, note?: string) {
    void run(
      `add-${String(amountMl)}`,
      async () => {
        const id = await log.mutateAsync({ date, amountMl, ...(note ? { note } : {}) });
        let available = true;
        toast.success(t('hydration.log_success', { amount: amountMl }), {
          ...(id
            ? {
                action: {
                  label: t('hydration.undo'),
                  onClick: () => {
                    if (!available) return;
                    available = false;
                    void remove(id);
                  },
                },
              }
            : {}),
        });
      },
      'hydration.log_error',
    );
  }

  return { add, remove, pendingKeys };
}
