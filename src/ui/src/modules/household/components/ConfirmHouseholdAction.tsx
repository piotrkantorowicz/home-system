import {
  Banner,
  Button,
  Dialog,
  DialogContent,
  DialogTitle,
  DialogDescription,
} from '@shared/components/ui';
import { useTranslation } from 'react-i18next';

import { useHouseholdMutation } from '../api/queries';

interface ConfirmHouseholdActionProps {
  title: string;
  description: string;
  action: Parameters<ReturnType<typeof useHouseholdMutation>['mutate']>[0];
  onClose: () => void;
}

export function ConfirmHouseholdAction({
  title,
  description,
  action,
  onClose,
}: ConfirmHouseholdActionProps) {
  const { t } = useTranslation('household');
  const mutation = useHouseholdMutation();
  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open && !mutation.isPending) onClose();
      }}
    >
      <DialogContent aria-describedby="confirm-description">
        <DialogTitle>{title}</DialogTitle>
        <DialogDescription id="confirm-description">{description}</DialogDescription>
        {mutation.isError && <Banner variant="error">{t('save_error')}</Banner>}
        <div className="flex justify-end gap-2">
          <Button variant="ghost" disabled={mutation.isPending} onClick={onClose}>
            {t('cancel')}
          </Button>
          <Button
            variant="destructive"
            disabled={mutation.isPending}
            onClick={() => {
              mutation.mutate(action, { onSuccess: onClose });
            }}
          >
            {t(mutation.isPending ? 'saving' : 'confirm')}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
