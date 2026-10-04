import { cn } from '@shared/lib/utils';

export interface DailyBar {
  /** Stable key, usually the ISO date. */
  key: string;
  /** Weekday label. */
  label: string;
  /** Date label under the weekday. */
  sublabel: string;
  /** `null` = nothing logged (distinct from a logged `0`). */
  value: number | null;
  /** Pre-formatted figure shown above the bar. */
  text: string;
  over?: boolean;
  isToday?: boolean;
  /** Full text equivalent for screen readers. */
  srText: string;
}

export interface DailyBarsProps {
  days: DailyBar[];
  /** Draws a dashed target line when set. */
  target?: number | null;
  targetLabel?: string;
  overLabel: string;
  missingText: string;
  ariaLabel: string;
  /** Long ranges (30/90 days): no figure above each bar and a tighter gap; blank labels stay blank. */
  compact?: boolean;
  /** Bar colour family; `water` for hydration. Over-target days always use the destructive colour. */
  tone?: 'primary' | 'water';
  className?: string;
}

const PLOT_H = 120;
const LABEL_H = 18;

/** Labelled daily bars with a target line and a highlighted today. Visuals are `aria-hidden`; the list is the accessible equivalent. */
export function DailyBars({
  days,
  target = null,
  targetLabel,
  overLabel,
  missingText,
  ariaLabel,
  compact = false,
  tone = 'primary',
  className,
}: DailyBarsProps) {
  const scaleMax = Math.max(target ?? 0, ...days.map((d) => d.value ?? 0), 1);
  const px = (v: number) => (v / scaleMax) * PLOT_H;

  return (
    <figure className={cn('m-0 min-w-0', className)} aria-label={ariaLabel}>
      <div aria-hidden="true" className="relative border-b" style={{ height: PLOT_H + LABEL_H }}>
        {target !== null && (
          <div
            data-testid="target-line"
            className="border-text-2 absolute inset-x-0 border-t border-dashed"
            style={{ bottom: px(target) }}
          />
        )}
        <div
          className={cn(
            'relative flex h-full items-end',
            compact ? 'gap-px' : 'gap-1.5 sm:gap-2.5',
          )}
        >
          {days.map((day) => (
            <div key={day.key} className="flex min-w-0 flex-1 flex-col items-center justify-end">
              <span
                className={cn(
                  compact && 'hidden',
                  'numeral text-11px mb-0.5 leading-none whitespace-nowrap',
                  day.over && 'text-destructive font-bold',
                  day.value === null && 'text-text-3',
                )}
              >
                {day.value === null ? missingText : `${day.over ? '▲ ' : ''}${day.text}`}
              </span>
              <div
                className={cn(
                  'w-full rounded-t-md',
                  !compact && 'max-w-[46px]',
                  day.value === null && 'border-border-strong border border-dashed',
                )}
                style={{
                  height: day.value === null ? 2 : Math.max(2, px(day.value)),
                  background:
                    day.value === null
                      ? 'transparent'
                      : day.over
                        ? 'var(--color-destructive)'
                        : day.isToday
                          ? `var(--color-${tone})`
                          : `color-mix(in oklab, var(--color-${tone}) 32%, transparent)`,
                }}
              />
            </div>
          ))}
        </div>
      </div>
      <div
        aria-hidden="true"
        className={cn('mt-1.5 flex', compact ? 'gap-px' : 'gap-1.5 sm:gap-2.5')}
      >
        {days.map((day) => (
          <div key={day.key} className="min-w-0 flex-1 text-center">
            <div
              className={cn(
                'text-xs',
                day.isToday ? 'text-foreground font-bold' : 'text-muted-foreground',
              )}
            >
              {day.label}
            </div>
            <div className="text-text-2 text-11px">{day.sublabel}</div>
          </div>
        ))}
      </div>
      {targetLabel && target !== null && (
        <p aria-hidden="true" className="text-text-2 mt-2 flex items-center gap-2 text-xs">
          <span className="border-text-2 w-5 border-t border-dashed" />
          {targetLabel}
        </p>
      )}
      <ul className="sr-only">
        {days.map((day) => (
          <li key={day.key}>
            {day.srText}
            {day.over ? ` (${overLabel})` : ''}
          </li>
        ))}
      </ul>
    </figure>
  );
}
