import { cn } from '@shared/lib/utils';

import type { CSSProperties, ReactNode } from 'react';

export interface RingProps {
  /** 0–100. Clamped. */
  percent: number;
  size?: number;
  thickness?: number;
  color?: 'primary' | 'fat';
  children: ReactNode;
  className?: string;
}

export function Ring({
  percent,
  size = 176,
  thickness = 15,
  color = 'primary',
  children,
  className,
}: RingProps) {
  const pct = Math.min(100, Math.max(0, percent));
  const track = 'var(--color-muted)';
  const fill = color === 'fat' ? 'var(--color-destructive)' : 'var(--color-primary)';

  const style: CSSProperties = {
    width: size,
    height: size,
    background: `conic-gradient(${fill} 0 ${String(pct)}%, ${track} ${String(pct)}% 100%)`,
  };

  return (
    <div
      className={cn('relative grid flex-none place-items-center rounded-full', className)}
      style={style}
    >
      <div className="bg-card absolute rounded-full" style={{ inset: thickness }} />
      <div className="relative text-center">{children}</div>
    </div>
  );
}
