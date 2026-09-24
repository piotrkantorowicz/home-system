import { cn } from '@shared/lib/utils';
import { CircleAlert, CircleCheck, Info, TriangleAlert } from 'lucide-react';

import { Button } from './Button';

import type { LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';

export type BannerVariant = 'success' | 'warning' | 'error' | 'info';

export interface BannerProps {
  variant: BannerVariant;
  title?: string;
  children?: ReactNode;
  /** Renders a ghost "Retry" button (error variant). */
  onRetry?: () => void;
  retryLabel?: string;
  className?: string;
}

const config: Record<BannerVariant, { token: string; icon: LucideIcon }> = {
  success: { token: '--color-good', icon: CircleCheck },
  warning: { token: '--color-carbs', icon: TriangleAlert },
  error: { token: '--color-fat', icon: CircleAlert },
  info: { token: '--color-primary', icon: Info },
};

export function Banner({
  variant,
  title,
  children,
  onRetry,
  retryLabel = 'Retry',
  className,
}: BannerProps) {
  const { token, icon: Icon } = config[variant];

  return (
    <div
      role={variant === 'error' ? 'alert' : 'status'}
      className={cn('rounded-16px flex gap-3 border p-4', className)}
      style={{
        background: `color-mix(in oklab, hsl(var(${token})) 12%, transparent)`,
        borderColor: `color-mix(in oklab, hsl(var(${token})) 28%, transparent)`,
      }}
    >
      <Icon
        className="mt-0.5 size-4 shrink-0"
        style={{ color: `hsl(var(${token}))` }}
        strokeWidth={2.2}
      />
      <div className="text-12-5px min-w-0 flex-1">
        {title ? (
          <div className="font-bold" style={{ color: `hsl(var(${token}))` }}>
            {title}
          </div>
        ) : null}
        {children ? <div className={cn(title && 'mt-1', 'text-text-2')}>{children}</div> : null}
        {onRetry ? (
          <Button size="xs" variant="ghost" className="mt-2 -ml-2" onClick={onRetry}>
            {retryLabel}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
