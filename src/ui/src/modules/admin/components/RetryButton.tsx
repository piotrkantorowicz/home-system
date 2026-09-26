import { Button } from '@shared/components/ui';
import { RotateCcw } from 'lucide-react';
import { useTranslation } from 'react-i18next';

interface RetryButtonProps {
  pending: boolean;
  onRetry: () => void;
}

export function RetryButton({ pending, onRetry }: RetryButtonProps) {
  const { t } = useTranslation('admin');
  return (
    <Button type="button" variant="secondary" size="sm" disabled={pending} onClick={onRetry}>
      <RotateCcw className="size-3.5" aria-hidden />
      {t('retry')}
    </Button>
  );
}
