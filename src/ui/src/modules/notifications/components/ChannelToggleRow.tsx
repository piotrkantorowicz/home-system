import { Switch } from '@shared/components/ui';
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
  return (
    <div
      className={cn(
        'border-border bg-card flex items-center justify-between gap-4 rounded-[13px] border p-4',
        disabled && 'opacity-60',
      )}
      title={disabled && disabledReason ? disabledReason : undefined}
    >
      <div className="flex min-w-0 items-center gap-3">
        <span className="bg-accent text-primary flex size-9 shrink-0 items-center justify-center rounded-[10px]">
          <Icon aria-hidden className="size-[18px]" />
        </span>
        <div className="min-w-0">
          <p className="text-sm font-medium">
            {label}
            {disabled && disabledReason ? (
              <span className="text-muted-foreground ml-2 text-xs font-normal">
                {disabledReason}
              </span>
            ) : null}
          </p>
          <p className="text-muted-foreground mt-0.5 text-sm">{description}</p>
        </div>
      </div>

      <Switch
        id={id}
        checked={checked}
        disabled={disabled}
        aria-label={label}
        onCheckedChange={(next) => {
          onChange?.(next);
        }}
      />
    </div>
  );
}
