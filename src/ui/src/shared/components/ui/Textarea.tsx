import { cn } from '@shared/lib/utils';
import { forwardRef } from 'react';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, ...props }, ref) => {
    return (
      <textarea
        className={cn(
          'border-border bg-secondary placeholder:text-muted-foreground/70 aria-[invalid=true]:border-destructive flex min-h-[100px] w-full resize-y rounded-[13px] border px-3 py-2.5 text-[13px] transition-colors duration-150 focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50',
          className,
        )}
        ref={ref}
        {...props}
      />
    );
  },
);

Textarea.displayName = 'Textarea';
