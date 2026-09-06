import { cn, formatNumber } from '@shared/lib/utils';

import { Skeleton } from './Skeleton';

export type Macro = 'protein' | 'carbs' | 'fat' | 'fiber' | 'primary';

export interface MacroBarProps {
  label: string;
  value: number;
  target: number;
  unit?: string;
  macro: Macro;
  /** Override the right-aligned "value / target unit" text. */
  valueText?: string;
  loading?: boolean;
  className?: string;
}

const fillClass: Record<Macro, string> = {
  protein: 'bg-protein',
  carbs: 'bg-carbs',
  fat: 'bg-fat',
  fiber: 'bg-fiber',
  primary: 'bg-primary',
};

export function MacroBar({
  label,
  value,
  target,
  unit = 'g',
  macro,
  valueText,
  loading = false,
  className,
}: MacroBarProps) {
  const pct = target > 0 ? Math.min(100, Math.max(0, (value / target) * 100)) : 0;
  const over = target > 0 && value > target;

  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      <div className="flex justify-between text-[12.5px]">
        <span className="font-semibold">{label}</span>
        <span className="text-text-2 tnum">
          {valueText ?? `${formatNumber(value)} / ${formatNumber(target)}${unit ? ` ${unit}` : ''}`}
        </span>
      </div>
      {loading ? (
        <Skeleton className="h-2 w-full rounded-full" />
      ) : (
        <div className="bg-muted relative h-2 overflow-hidden rounded-full">
          <div
            className={cn(
              'h-full rounded-full motion-safe:transition-[width] motion-safe:duration-500 motion-safe:ease-out',
              fillClass[macro],
            )}
            style={{ width: `${String(pct)}%` }}
          />
          {over ? <span className="bg-fat absolute inset-y-0 right-0 w-0.5" /> : null}
        </div>
      )}
    </div>
  );
}
