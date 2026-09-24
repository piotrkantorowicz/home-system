import { cn } from '@shared/lib/utils';

export interface SwitchProps {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled?: boolean;
  id?: string;
  'aria-label'?: string;
  'aria-labelledby'?: string;
  className?: string;
}

export function Switch({
  checked,
  onCheckedChange,
  disabled = false,
  id,
  className,
  ...aria
}: SwitchProps) {
  return (
    <button
      type="button"
      role="switch"
      id={id}
      aria-checked={checked}
      aria-label={aria['aria-label']}
      aria-labelledby={aria['aria-labelledby']}
      aria-disabled={disabled || undefined}
      disabled={disabled}
      onClick={() => {
        onCheckedChange(!checked);
      }}
      className={cn(
        'focus-visible:ring-ring w-42px p-3px flex h-6 flex-none items-center rounded-full transition-colors duration-150 focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50',
        checked
          ? 'bg-primary justify-end'
          : 'bg-border-strong dark:bg-muted dark:border-border-strong justify-start dark:border',
        className,
      )}
    >
      <span
        className={cn(
          'size-18px rounded-full bg-white shadow-sm ring-1 ring-black/5',
          checked && 'ring-black/10',
        )}
      />
    </button>
  );
}
