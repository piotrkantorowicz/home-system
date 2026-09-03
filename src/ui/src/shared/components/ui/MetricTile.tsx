import { cn } from '@shared/lib/utils';

import { Skeleton } from './Skeleton';

import type { ReactNode } from 'react';

type MetricTileAccent = 'default' | 'good' | 'fat' | 'primary';

export interface MetricTileProps {
  label: string;
  value: ReactNode;
  /** Small trailing line — a unit ("kcal") or a Polish sub-label. */
  hint?: string;
  accent?: MetricTileAccent;
  loading?: boolean;
  className?: string;
}

const accentValueClass: Record<MetricTileAccent, string> = {
  default: 'text-foreground',
  good: 'text-success',
  fat: 'text-destructive',
  primary: 'text-primary',
};

const accentTint: Record<MetricTileAccent, string | undefined> = {
  default: undefined,
  good: 'color-mix(in oklab, hsl(var(--color-good)) 12%, transparent)',
  fat: 'color-mix(in oklab, hsl(var(--color-fat)) 12%, transparent)',
  primary: 'color-mix(in oklab, hsl(var(--color-primary)) 10%, transparent)',
};

export function MetricTile({
  label,
  value,
  hint,
  accent = 'default',
  loading = false,
  className,
}: MetricTileProps) {
  return (
    <div
      className={cn(
        'border-border rounded-[15px] border p-3.5',
        accent === 'default' && 'bg-secondary',
        className,
      )}
      style={accent === 'default' ? undefined : { background: accentTint[accent] }}
    >
      <div className="text-muted-foreground text-[10.5px] font-semibold tracking-[0.05em] uppercase">
        {label}
      </div>
      {loading ? (
        <Skeleton className="mt-1 h-5 w-16" />
      ) : (
        <div className={cn('numeral mt-0.5 text-[19px] font-bold', accentValueClass[accent])}>
          {value}
        </div>
      )}
      {hint ? <div className="text-muted-foreground text-[10.5px]">{hint}</div> : null}
    </div>
  );
}
