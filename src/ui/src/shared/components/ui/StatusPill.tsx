import { cn } from '@shared/lib/utils';

import type { LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';

export type StatusPillVariant = 'good' | 'over' | 'neutral';

export interface StatusPillProps {
  variant: StatusPillVariant;
  icon?: LucideIcon;
  children: ReactNode;
  className?: string;
}

const variantText: Record<StatusPillVariant, string> = {
  good: 'text-success',
  over: 'text-destructive',
  neutral: 'text-muted-foreground',
};

const variantBg: Record<StatusPillVariant, string> = {
  good: 'color-mix(in oklab, var(--color-good) 14%, transparent)',
  over: 'color-mix(in oklab, var(--color-fat) 14%, transparent)',
  neutral: 'var(--color-muted)',
};

export function StatusPill({ variant, icon: Icon, children, className }: StatusPillProps) {
  return (
    <span
      className={cn(
        'inline-flex h-[30px] items-center gap-1.5 rounded-full px-3 text-xs font-bold',
        variantText[variant],
        className,
      )}
      style={{ background: variantBg[variant] }}
    >
      {Icon ? <Icon className="size-3.5" strokeWidth={2.6} /> : null}
      {children}
    </span>
  );
}
