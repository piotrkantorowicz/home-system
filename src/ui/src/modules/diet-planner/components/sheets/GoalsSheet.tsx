import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@shared/components/ui';
import { useTranslation } from 'react-i18next';

import { GoalsForm } from '../settings';

export interface GoalsSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function GoalsSheet({ open, onOpenChange }: GoalsSheetProps) {
  const { t } = useTranslation();
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right">
        <SheetHeader>
          <SheetTitle>{t('sheets.goals.title')}</SheetTitle>
          <SheetDescription>{t('sheets.goals.description')}</SheetDescription>
        </SheetHeader>
        <div className="mt-6 overflow-y-auto">
          <GoalsForm
            onSuccess={() => {
              onOpenChange(false);
            }}
          />
        </div>
      </SheetContent>
    </Sheet>
  );
}
