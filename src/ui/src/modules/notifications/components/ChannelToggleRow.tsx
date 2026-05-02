import { cn } from '@shared/lib/utils';

import type { LucideIcon } from 'lucide-react';

export interface ChannelToggleRowProps {
  id: string;
  label: string;
  description: string;
  icon: LucideIcon;
  checked: boolean;
  disabled?: boolean;
  disabledReason?: string;
  onChange?: (next: boolean) => void;
}

export function ChannelToggleRow({
  id,
  label,
  description,
  icon: Icon,
  checked,
  disabled = false,
  disabledReason,
  onChange,
}: ChannelToggleRowProps) {
  function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    if (!disabled) onChange?.(event.target.checked);
  }

  return (
    <div
      className={cn(
        'border-border bg-surface flex items-center justify-between gap-4 rounded-md border p-4',
        disabled && 'opacity-60',
      )}
      title={disabled && disabledReason ? disabledReason : undefined}
    >
      <div className="flex min-w-0 items-center gap-3">
        <Icon aria-hidden className="text-text-muted mt-0.5 size-5 shrink-0" />
        <div className="min-w-0">
          <p className="text-text text-sm font-medium">
            {label}
            {disabled && disabledReason ? (
              <span className="text-text-muted ml-2 text-xs font-normal">{disabledReason}</span>
            ) : null}
          </p>
          <p className="text-text-muted mt-0.5 text-sm">{description}</p>
        </div>
      </div>

      <input
        id={id}
        type="checkbox"
        role="switch"
        checked={checked}
        disabled={disabled}
        aria-disabled={disabled}
        aria-checked={checked}
        aria-label={label}
        onChange={handleChange}
        className={cn(
          'border-border bg-surface h-5 w-9 cursor-pointer appearance-none rounded-full border-2 transition-colors',
          'checked:bg-primary checked:border-primary',
          'focus-visible:ring-primary focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none',
          disabled && 'cursor-not-allowed',
        )}
      />
    </div>
  );
}
