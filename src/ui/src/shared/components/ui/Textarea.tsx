import { cn } from '@shared/lib/utils';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type -- REASON: Keep TextareaProps as a named public component type while inheriting native textarea props.
export interface TextareaProps extends React.ComponentProps<'textarea'> {}

export function Textarea({ ref, className, ...props }: TextareaProps) {
  return (
    <textarea
      className={cn(
        'border-border bg-secondary placeholder:text-muted-foreground/70 aria-[invalid=true]:border-destructive min-h-100px rounded-13px text-13px focus-visible:ring-ring flex w-full resize-y border px-3 py-2.5 transition-colors duration-150 focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50',
        className,
      )}
      ref={ref}
      {...props}
    />
  );
}
