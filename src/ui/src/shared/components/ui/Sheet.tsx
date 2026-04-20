import * as DialogPrimitive from '@radix-ui/react-dialog';
import { cn } from '@shared/lib/utils';
import { X } from 'lucide-react';
import * as React from 'react';

const Sheet = DialogPrimitive.Root;

export const SheetTrigger = DialogPrimitive.Trigger;
export type SheetTriggerProps = React.ComponentPropsWithoutRef<typeof DialogPrimitive.Trigger>;

export interface SheetHeaderProps {
  children: React.ReactNode;
  className?: string;
}

function SheetHeader({ children, className }: SheetHeaderProps) {
  return <div className={cn('flex flex-col space-y-1.5 p-6 pb-0', className)}>{children}</div>;
}
SheetHeader.displayName = 'SheetHeader';

export interface SheetTitleProps {
  children: React.ReactNode;
  className?: string;
}

function SheetTitle({ children, className }: SheetTitleProps) {
  return <h2 className={cn('text-lg font-semibold', className)}>{children}</h2>;
}
SheetTitle.displayName = 'SheetTitle';

export interface SheetDescriptionProps {
  children: React.ReactNode;
  className?: string;
}

function SheetDescription({ children, className }: SheetDescriptionProps) {
  return <p className={cn('text-muted-foreground text-sm', className)}>{children}</p>;
}
SheetDescription.displayName = 'SheetDescription';

export interface SheetContentProps extends React.ComponentPropsWithoutRef<
  typeof DialogPrimitive.Content
> {
  onClose: () => void;
  side?: 'left' | 'right';
}

const SheetContent = React.forwardRef<
  React.ComponentRef<typeof DialogPrimitive.Content>,
  SheetContentProps
>(({ className, children, onClose, side = 'left', ...props }, ref) => (
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
));
SheetContent.displayName = 'SheetContent';

export { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription };
