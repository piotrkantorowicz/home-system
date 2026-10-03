import { cn } from '@shared/lib/utils';

/** Numeric text: tabular figures so columns, meters and totals line up. */
export function Num({ className, ...props }: React.ComponentProps<'span'>) {
  return <span data-numeric className={cn('tabular-nums', className)} {...props} />;
}
