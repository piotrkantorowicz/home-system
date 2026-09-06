import { cn } from '@shared/lib/utils';
import { useId } from 'react';

export interface SegmentedControlOption<T extends string> {
  value: T;
  label: string;
}

export interface SegmentedControlProps<T extends string> {
  options: SegmentedControlOption<T>[];
  value: T;
  onChange: (value: T) => void;
  label: string;
  className?: string;
}

export function SegmentedControl<T extends string>({
  options,
  value,
  onChange,
  label,
  className,
}: SegmentedControlProps<T>) {
  const groupName = useId();

  const move = (dir: -1 | 1) => {
    const index = options.findIndex((o) => o.value === value);
    const next = options[(index + dir + options.length) % options.length];
    if (next) onChange(next.value);
  };

  return (
    <div
      role="radiogroup"
      aria-label={label}
      className={cn('border-border bg-muted flex rounded-[12px] border p-[3px]', className)}
    >
      {options.map((option) => {
        const active = option.value === value;
        return (
          <button
            key={option.value}
            type="button"
            role="radio"
            name={groupName}
            aria-checked={active}
            tabIndex={active ? 0 : -1}
            onClick={() => {
              onChange(option.value);
            }}
            onKeyDown={(event) => {
              if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
                event.preventDefault();
                move(1);
              } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
                event.preventDefault();
                move(-1);
              }
            }}
            className={cn(
              'rounded-[9px] px-3.5 py-[7px] text-[12.5px] transition-colors duration-150 focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:outline-none',
              active
                ? 'bg-card text-foreground font-bold shadow-sm'
                : 'text-text-2 hover:text-foreground font-semibold',
            )}
          >
            {option.label}
          </button>
        );
      })}
    </div>
  );
}
