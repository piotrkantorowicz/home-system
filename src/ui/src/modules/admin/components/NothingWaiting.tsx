import { Check } from 'lucide-react';
import { useTranslation } from 'react-i18next';

/** The one-line empty state of a failed-messages section. */
export function NothingWaiting() {
  const { t } = useTranslation('admin');
  return (
    <p className="text-good flex items-center gap-1.5 px-4 pb-4 text-sm md:px-6">
      <Check className="size-4" strokeWidth={2.6} aria-hidden />
      {t('nothing_waiting')}
    </p>
  );
}
