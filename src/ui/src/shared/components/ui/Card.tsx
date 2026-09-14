import { cn } from '@shared/lib/utils';
export function Card({ ref, className, ...props }: React.ComponentProps<'div'>) {
  return (
    <div
      ref={ref}
      className={cn(
        // Refresh: 22px radius (rounded-xl), 1px --border hairline, exactly one shadow level.
        'bg-card text-card-foreground rounded-xl border shadow-sm transition-colors duration-200',
        className,
      )}
      {...props}
    />
  );
}

export function CardHeader({ ref, className, ...props }: React.ComponentProps<'div'>) {
  return <div ref={ref} className={cn('flex flex-col space-y-2 p-6', className)} {...props} />;
}

export function CardTitle({ ref, className, children, ...props }: React.ComponentProps<'h3'>) {
  return (
    <h3
      ref={ref}
      className={cn('text-xl leading-tight font-semibold tracking-tight', className)}
      {...props}
    >
      {children}
    </h3>
  );
}

export function CardDescription({ ref, className, ...props }: React.ComponentProps<'p'>) {
  return (
    <p
      ref={ref}
      className={cn('text-muted-foreground text-[0.9rem] leading-relaxed', className)}
      {...props}
    />
  );
}

export function CardContent({ ref, className, ...props }: React.ComponentProps<'div'>) {
  return <div ref={ref} className={cn('p-6 pt-0', className)} {...props} />;
}

export function CardFooter({ ref, className, ...props }: React.ComponentProps<'div'>) {
  return <div ref={ref} className={cn('flex items-center p-6 pt-0', className)} {...props} />;
}
