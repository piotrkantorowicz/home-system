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

      <label
        htmlFor={id}
        className={cn(
          'relative inline-flex h-6 w-11 shrink-0 items-center rounded-full border-2 transition-colors',
          checked ? 'bg-primary border-primary' : 'bg-surface-alt border-border',
          disabled ? 'cursor-not-allowed' : 'cursor-pointer',
          'focus-within:ring-primary focus-within:ring-2 focus-within:ring-offset-2',
        )}
      >
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
          className="sr-only"
        />
        <span
          aria-hidden
          className={cn(
            'inline-block size-4 rounded-full bg-white shadow transition-transform',
            checked ? 'translate-x-5' : 'translate-x-0.5',
          )}
        />
      </label>
    </div>
  );
}
