import { cn } from '@shared/lib/utils';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type -- REASON: Keep CheckboxProps as a named public component type while inheriting native input props.
export interface CheckboxProps extends React.ComponentProps<'input'> {}

export function Checkbox({ ref, className, ...props }: CheckboxProps) {
  return (
    <input
      type="checkbox"
      className={cn(
        'border-input accent-primary h-4 w-4 rounded border transition-colors disabled:cursor-not-allowed disabled:opacity-50',
        className,
      )}
      ref={ref}
      {...props}
    />
  );
}
