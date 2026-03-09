import { forwardRef } from 'react';
import { cn } from '@shared/lib/utils';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export interface LabelProps extends React.LabelHTMLAttributes<HTMLLabelElement> {}

export const Label = forwardRef<HTMLLabelElement, LabelProps>(({ className, ...props }, ref) => (
  <label
    ref={ref}
    className={cn(
      'text-[0.9rem] font-medium leading-none mb-1.5 block peer-disabled:cursor-not-allowed peer-disabled:opacity-70',
      className
    )}
    {...props}
  />
));

Label.displayName = 'Label';
