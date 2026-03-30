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
      <div className="mb-1.5 flex justify-between text-sm">
        <span className="font-medium">{label}</span>
        <span className="text-muted-foreground">
          {unit === 'kcal' ? Math.round(actual) : actual.toFixed(1)}
          {unit}
          {goal !== null && (
            <span className={cn('ml-1 text-xs', exceeded ? 'text-destructive' : '')}>
              / {unit === 'kcal' ? Math.round(goal) : goal}
              {unit}
            </span>
          )}
        </span>
      </div>
      <div className="bg-muted h-2 overflow-hidden rounded-full">
        <div
          className={cn(
            'h-full rounded-full transition-all duration-500',
            exceeded ? 'bg-destructive' : `bg-gradient-to-r ${gradient}`,
          )}
          style={{ width: `${String(goal ? pct : 0)}%` }}
        />
      </div>
    </div>
  );
}
