import * as DialogPrimitive from '@radix-ui/react-dialog';
import { cn } from '@shared/lib/utils';
import { X } from 'lucide-react';
import * as React from 'react';

const Sheet = DialogPrimitive.Root;

export const SheetTrigger = DialogPrimitive.Trigger;
export type SheetTriggerProps = React.ComponentProps<typeof DialogPrimitive.Trigger>;

function SheetHeader({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('flex flex-col space-y-1.5 p-6 pb-0', className)} {...props} />;
}
SheetHeader.displayName = 'SheetHeader';

function SheetTitle({
  ref,
  className,
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Title>) {
  return (
    <DialogPrimitive.Title
      ref={ref}
      className={cn('text-lg font-semibold', className)}
      {...props}
    />
  );
}

function SheetDescription({
  ref,
  className,
  ...props
}: React.ComponentProps<typeof DialogPrimitive.Description>) {
  return (
    <DialogPrimitive.Description
      ref={ref}
      className={cn('text-muted-foreground text-sm', className)}
      {...props}
    />
  );
}

export interface SheetContentProps extends React.ComponentProps<typeof DialogPrimitive.Content> {
  onClose?: () => void;
  side?: 'left' | 'right';
}

function SheetContent({
  ref,
  className,
  children,
  onClose,
  side = 'left',
  ...props
}: SheetContentProps) {
  return (
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay
        className={cn(
          'data-[state=open]:animate-in data-[state=closed]:animate-out',
          'data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0',
          'fixed inset-0 z-50 bg-black/60 backdrop-blur-sm',
        )}
      />
      <DialogPrimitive.Content
        ref={ref}
        className={cn(
          'data-[state=open]:animate-in data-[state=closed]:animate-out',
          side === 'right'
            ? 'data-[state=closed]:slide-out-to-right data-[state=open]:slide-in-from-right fixed inset-y-0 right-0 z-50 h-full w-full max-w-lg duration-300'
            : 'data-[state=closed]:slide-out-to-left data-[state=open]:slide-in-from-left fixed inset-y-0 left-0 z-50 h-full w-64 duration-300',
          className,
        )}
        {...props}
      >
        {children}
        <DialogPrimitive.Close
          onClick={onClose}
          className="ring-offset-background hover:bg-accent focus:ring-ring absolute top-4 right-4 rounded-lg p-1 opacity-70 transition-all duration-200 hover:opacity-100 focus:ring-2 focus:ring-offset-2 focus:outline-none"
          aria-label="Close"
        >
          <X className="h-4 w-4" />
          <span className="sr-only">Close</span>
        </DialogPrimitive.Close>
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  );
}

export type SheetHeaderProps = React.HTMLAttributes<HTMLDivElement>;
export type SheetTitleProps = React.ComponentProps<typeof DialogPrimitive.Title>;
export type SheetDescriptionProps = React.ComponentProps<typeof DialogPrimitive.Description>;

export { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription };
