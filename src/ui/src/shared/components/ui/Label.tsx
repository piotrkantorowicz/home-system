import { cn } from '@shared/lib/utils';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type -- REASON: Keep LabelProps as a named public component type while inheriting native label props.
export interface LabelProps extends React.ComponentProps<'label'> {}

export function Label({ ref, className, ...props }: LabelProps) {
  return (
    // eslint-disable-next-line jsx-a11y/label-has-associated-control -- REASON: Consumers provide htmlFor and label text through props, so the label-control association is established at each usage site.
    <label
      ref={ref}
      className={cn(
        'text-0-9rem mb-1.5 block leading-none font-medium peer-disabled:cursor-not-allowed peer-disabled:opacity-70',
        className,
      )}
      {...props}
    />
  );
}
