import { MealScheduleForm } from '../settings';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@shared/components/ui';
import { useTranslation } from 'react-i18next';

export interface MealScheduleSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function MealScheduleSheet({ open, onOpenChange }: MealScheduleSheetProps) {
  const { t } = useTranslation();
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right">
        <SheetHeader>
          <SheetTitle>{t('sheets.meal_schedule.title')}</SheetTitle>
          <SheetDescription>{t('sheets.meal_schedule.description')}</SheetDescription>
        </SheetHeader>
        <div className="mt-6 overflow-y-auto">
          <MealScheduleForm onSuccess={() => onOpenChange(false)} />
        </div>
      </SheetContent>
    </Sheet>
  );
}
