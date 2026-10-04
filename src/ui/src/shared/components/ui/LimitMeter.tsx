import { cn } from '@shared/lib/utils';

export interface LimitMeterProps {
  /** Share of the limit already used, 0-100. */
  percent: number;
  /** Past the limit: red fill, with a tick where the limit sits. */
  over?: boolean;
  /** With `over`, where the limit sits on the bar, 0-100. */
  limitAt?: number;
  className?: string;
}

/** A decorative bar; the caller writes the status in text so meaning never rests on colour. */
export function LimitMeter({ percent, over = false, limitAt = 100, className }: LimitMeterProps) {
  return (
    <div
      aria-hidden="true"
      className={cn('bg-muted relative h-2 overflow-visible rounded-full', className)}
    >
      <div
        className={cn('h-full rounded-full', over ? 'bg-destructive' : 'bg-primary')}
        style={{ width: `${String(Math.min(100, Math.max(0, percent)))}%` }}
      />
      {over && (
        <div
          className="bg-foreground absolute -top-1 h-4 w-0.5 rounded-full"
          style={{ left: `${String(Math.min(100, Math.max(0, limitAt)))}%` }}
        />
      )}
    </div>
  );
}
