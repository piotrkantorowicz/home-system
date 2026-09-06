import { Slot } from '@radix-ui/react-slot';
import { cn } from '@shared/lib/utils';
import { cva, type VariantProps } from 'class-variance-authority';
import { forwardRef } from 'react';

const buttonVariants = cva(
  'inline-flex items-center justify-center whitespace-nowrap rounded-lg text-[0.9rem] font-medium transition-all duration-200 ease-out focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 active:scale-[0.97]',
  {
    variants: {
      variant: {
        default:
          'bg-primary text-primary-foreground shadow-md shadow-primary/20 hover:shadow-lg hover:shadow-primary/30 hover:brightness-110 hover:-translate-y-0.5',
        destructive:
          'bg-destructive text-destructive-foreground shadow-md shadow-destructive/20 hover:shadow-lg hover:shadow-destructive/30 hover:brightness-110 hover:-translate-y-0.5',
        outline:
          'border border-input bg-background hover:bg-accent hover:text-accent-foreground hover:border-primary/40 hover:-translate-y-0.5 hover:shadow-sm',
        secondary:
          'bg-secondary text-secondary-foreground hover:bg-secondary/80 hover:-translate-y-0.5',
        ghost: 'hover:bg-accent hover:text-accent-foreground',
        link: 'text-primary underline-offset-4 hover:underline',
      },
      size: {
        default: 'h-11 px-5 py-2.5',
        sm: 'h-9 rounded-md px-3.5 text-sm',
        lg: 'h-12 rounded-lg px-8 text-base',
        icon: 'h-10 w-10',
        // Refresh scale — primary CTA / row action / chip.
        xl: 'h-[42px] gap-2 rounded-[13px] px-4 text-[13.5px]',
        chip: 'h-8 gap-1.5 rounded-[10px] px-3.5 text-[12.5px]',
        xs: 'h-[30px] gap-1.5 rounded-[10px] px-3 text-[12.5px]',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  },
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>, VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : 'button';
    return (
      <Comp className={cn(buttonVariants({ variant, size, className }))} ref={ref} {...props} />
    );
  },
);

Button.displayName = 'Button';
