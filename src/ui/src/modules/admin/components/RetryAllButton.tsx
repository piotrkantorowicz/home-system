import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@shared/components/ui';
import { RotateCcw } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

interface RetryAllButtonProps {
  /** Dead letters the retry covers; the confirm dialog names it. */
  count: number;
  pending: boolean;
  onConfirm: () => void;
}

/** "Retry all" for one dead-letter table, behind a confirm dialog. */
export function RetryAllButton({ count, pending, onConfirm }: RetryAllButtonProps) {
  const { t } = useTranslation('admin');
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button
        type="button"
        variant="secondary"
        size="sm"
        disabled={pending || count === 0}
        onClick={() => {
          setOpen(true);
        }}
      >
        <RotateCcw className="size-3.5" aria-hidden />
        {t('retry_all')}
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('retry_all_confirm_title', { count })}</DialogTitle>
            <DialogDescription>{t('retry_all_confirm_body')}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => {
                setOpen(false);
              }}
            >
              {t('cancel')}
            </Button>
            <Button
              onClick={() => {
                setOpen(false);
                onConfirm();
              }}
            >
              {t('retry_all')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
