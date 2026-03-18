import { cn } from '@shared/lib/utils';

interface MacroProgressBarProps {
  label: string;
  actual: number;
  goal: number | null;
  unit?: string;
  gradient: string;
  className?: string;
}

export function MacroProgressBar({
  label,
  actual,
  goal,
  unit = 'g',
  gradient,
  className,
}: MacroProgressBarProps) {
  const pct = goal && goal > 0 ? Math.min((actual / goal) * 100, 100) : 0;
  const exceeded = goal !== null && goal > 0 && actual > goal;

  return (
    <div className={className}>
      <div className="flex justify-between text-sm mb-1.5">
        <span className="font-medium">{label}</span>
        <span className="text-muted-foreground">
          {unit === 'kcal' ? Math.round(actual) : actual.toFixed(1)}
          {unit}
          {goal !== null && (
            <span className={cn('text-xs ml-1', exceeded ? 'text-destructive' : '')}>
              / {unit === 'kcal' ? Math.round(goal) : goal}
              {unit}
            </span>
          )}
        </span>
      </div>
      <div className="h-2 bg-muted rounded-full overflow-hidden">
        <div
          className={cn(
            'h-full rounded-full transition-all duration-500',
            exceeded ? 'bg-destructive' : `bg-gradient-to-r ${gradient}`
          )}
          style={{ width: `${goal ? pct : 0}%` }}
        />
      </div>
    </div>
  );
}
