import { cn } from '@shared/lib/utils';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type -- REASON: Keep InputProps as a named public component type while inheriting native input props.
export interface InputProps extends React.ComponentProps<'input'> {}

export function Input({ ref, className, type, ...props }: InputProps) {
  return (
    <input
      type={type}
      className={cn(
        'border-border bg-secondary placeholder:text-muted-foreground/70 aria-[invalid=true]:border-destructive flex h-[42px] w-full rounded-[13px] border px-3 text-[13px] [color-scheme:light] transition-colors duration-150 file:border-0 file:bg-transparent file:text-sm file:font-medium focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50 dark:[color-scheme:dark]',
        className,
      )}
      ref={ref}
      {...props}
    />
  );
}
