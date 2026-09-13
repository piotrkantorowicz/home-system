import { cn } from '@shared/lib/utils';
export function Table({ ref, className, ...props }: React.ComponentProps<'table'>) {
  return (
    <div className="relative w-full overflow-auto">
      <table
        ref={ref}
        className={cn('w-full caption-bottom text-[0.9rem]', className)}
        {...props}
      />
    </div>
  );
}

export function TableHeader({ ref, className, ...props }: React.ComponentProps<'thead'>) {
  return <thead ref={ref} className={cn('[&_tr]:border-b', className)} {...props} />;
}

export function TableBody({ ref, className, ...props }: React.ComponentProps<'tbody'>) {
  return <tbody ref={ref} className={cn('[&_tr:last-child]:border-0', className)} {...props} />;
}

export function TableFooter({ ref, className, ...props }: React.ComponentProps<'tfoot'>) {
  return (
    <tfoot
      ref={ref}
      className={cn('bg-muted/50 border-t font-medium [&>tr]:last:border-b-0', className)}
      {...props}
    />
  );
}

export function TableRow({ ref, className, ...props }: React.ComponentProps<'tr'>) {
  return (
    <tr
      ref={ref}
      className={cn(
        'hover:bg-accent/50 data-[state=selected]:bg-accent border-b transition-colors duration-150',
        className,
      )}
      {...props}
    />
  );
}

export function TableHead({ ref, className, ...props }: React.ComponentProps<'th'>) {
  return (
    <th
      ref={ref}
      className={cn(
        'text-muted-foreground h-12 px-4 text-left align-middle text-xs font-semibold tracking-wider uppercase [&:has([role=checkbox])]:pr-0',
        className,
      )}
      {...props}
    />
  );
}

export function TableCell({ ref, className, ...props }: React.ComponentProps<'td'>) {
  return (
    <td
      ref={ref}
      className={cn('p-4 align-middle [&:has([role=checkbox])]:pr-0', className)}
      {...props}
    />
  );
}

export function TableCaption({ ref, className, ...props }: React.ComponentProps<'caption'>) {
  return (
    <caption ref={ref} className={cn('text-muted-foreground mt-4 text-sm', className)} {...props} />
  );
}
