import { cn } from '@shared/lib/utils';

import type { ReactNode } from 'react';

export interface FieldProps {
  id: string;
  label: string;
  error?: string | undefined;
  hint?: string | undefined;
  labelClassName?: string | undefined;
  className?: string | undefined;
  children: ReactNode;
}

/**
 * Label + control + error/hint line. One vertical rhythm for every form in the
 * app. Pair with the token `Input` / `Select` / `Textarea` primitives.
 */
export function Field({ id, label, error, hint, labelClassName, className, children }: FieldProps) {
  return (
    <div className={className}>
      <label
        htmlFor={id}
        className={cn(
          'mb-1 block text-[12px] font-semibold',
          error ? 'text-destructive' : (labelClassName ?? 'text-text-2'),
        )}
      >
        {label}
      </label>
      {children}
      {error ? (
        <p className="text-destructive mt-1 text-[11.5px]">{error}</p>
      ) : hint ? (
        <p className="text-muted-foreground mt-1 text-[11.5px]">{hint}</p>
      ) : null}
    </div>
  );
}
