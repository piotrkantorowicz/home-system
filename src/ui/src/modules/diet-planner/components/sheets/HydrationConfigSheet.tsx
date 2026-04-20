import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@shared/components/ui';
import { useTranslation } from 'react-i18next';

import { HydrationConfigForm } from '../settings';

export interface HydrationConfigSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function HydrationConfigSheet({ open, onOpenChange }: HydrationConfigSheetProps) {
  const { t } = useTranslation();
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right">
        <SheetHeader>
          <SheetTitle>{t('sheets.hydration.title')}</SheetTitle>
          <SheetDescription>{t('sheets.hydration.description')}</SheetDescription>
        </SheetHeader>
        <div className="mt-6 overflow-y-auto">
          <HydrationConfigForm
            onSuccess={() => {
              onOpenChange(false);
            }}
          />
        </div>
      </SheetContent>
    </Sheet>
  );
}
