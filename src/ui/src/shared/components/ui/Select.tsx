import { cn } from '@shared/lib/utils';
import { ChevronDown } from 'lucide-react';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type -- REASON: Keep SelectProps as a named public component type while inheriting native select props.
export interface SelectProps extends React.ComponentProps<'select'> {}

export function Select({ ref, className, children, ...props }: SelectProps) {
  return (
    <div className="relative w-full">
      <select
        className={cn(
          'border-border bg-secondary aria-[invalid=true]:border-destructive h-42px rounded-13px text-13px focus-visible:ring-ring flex w-full appearance-none border px-3 [color-scheme:light] transition-colors duration-150 focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50 dark:[color-scheme:dark]',
          className,
        )}
        ref={ref}
        {...props}
      >
        {children}
      </select>
      <ChevronDown className="text-muted-foreground pointer-events-none absolute top-1/2 right-3 h-4 w-4 -translate-y-1/2" />
    </div>
  );
}
