import { cn } from '@shared/lib/utils';
import { forwardRef } from 'react';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, ...props }, ref) => {
    return (
      <input
        type={type}
        className={cn(
          'border-input bg-background ring-offset-background placeholder:text-muted-foreground/70 focus-visible:ring-ring flex h-11 w-full rounded-lg border px-4 py-2.5 text-[0.9rem] [color-scheme:light] transition-all duration-200 file:border-0 file:bg-transparent file:text-sm file:font-medium focus-visible:border-transparent focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50 dark:[color-scheme:dark]',
          className,
        )}
        ref={ref}
        {...props}
      />
    );
  },
);

Input.displayName = 'Input';
