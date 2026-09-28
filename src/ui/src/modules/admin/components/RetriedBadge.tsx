import { Badge } from '@shared/components/ui';
import { useTranslation } from 'react-i18next';

interface RetriedBadgeProps {
  retryOf: string | null;
}

/** Marks a dead letter that is itself an earlier admin retry; the original is kept as history. */
export function RetriedBadge({ retryOf }: RetriedBadgeProps) {
  const { t } = useTranslation('admin');
  if (!retryOf) return null;
  return (
    <Badge variant="secondary" className="ml-2" title={t('retry_of', { id: retryOf })}>
      {t('retried')}
    </Badge>
  );
}
