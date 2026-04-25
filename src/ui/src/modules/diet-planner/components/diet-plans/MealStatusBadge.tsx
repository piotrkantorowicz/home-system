import { cn } from '@shared/lib/utils';
import { Check, Clock, Pencil } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export type MealStatus = 'Planned' | 'Done' | 'Modified';

export interface MealStatusBadgeProps {
  status: MealStatus;
  className?: string;
}

const STATUS_STYLES: Record<MealStatus, string> = {
  Planned: 'text-muted-foreground bg-muted/40 ring-border',
  Done: 'text-emerald-700 bg-emerald-50 ring-emerald-300 dark:text-emerald-300 dark:bg-emerald-950/40 dark:ring-emerald-700',
  Modified:
    'text-amber-700 bg-amber-50 ring-amber-300 dark:text-amber-300 dark:bg-amber-950/40 dark:ring-amber-700',
};

const STATUS_ICONS: Record<MealStatus, typeof Clock> = {
  Planned: Clock,
  Done: Check,
  Modified: Pencil,
};

export function MealStatusBadge({ status, className }: MealStatusBadgeProps) {
  const { t } = useTranslation();
  const Icon = STATUS_ICONS[status];
  const label = t(`calendar.meal_status.${status.toLowerCase()}`);

  return (
    <span
      className={cn(
        'inline-flex h-4 w-4 items-center justify-center rounded-full ring-1 ring-inset',
        STATUS_STYLES[status],
        className,
      )}
      aria-label={label}
      title={label}
    >
      <Icon className="h-3 w-3" />
    </span>
  );
}
