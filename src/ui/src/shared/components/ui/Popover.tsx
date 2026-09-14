import * as PopoverPrimitive from '@radix-ui/react-popover';
import { cn } from '@shared/lib/utils';
import * as React from 'react';

const Popover = PopoverPrimitive.Root;
const PopoverTrigger = PopoverPrimitive.Trigger;
const PopoverAnchor = PopoverPrimitive.Anchor;

function PopoverContent({
  ref,
  className,
  align = 'start',
  sideOffset = 8,
  ...props
}: React.ComponentProps<typeof PopoverPrimitive.Content>) {
  return (
    <PopoverPrimitive.Portal>
      <PopoverPrimitive.Content
        ref={ref}
        align={align}
        sideOffset={sideOffset}
        className={cn(
          'data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 z-50 w-72 rounded-[18px] border p-4 shadow-lg outline-none',
          className,
        )}
        style={{
          background: 'var(--color-secondary)',
          color: 'var(--color-foreground)',
          borderColor: 'var(--color-border)',
          boxShadow: 'var(--shadow-lg)',
        }}
        {...props}
      />
    </PopoverPrimitive.Portal>
  );
}

export { Popover, PopoverTrigger, PopoverAnchor, PopoverContent };
