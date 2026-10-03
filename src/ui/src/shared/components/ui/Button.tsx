import { Slot } from '@radix-ui/react-slot';
import { cn } from '@shared/lib/utils';
import { cva, type VariantProps } from 'class-variance-authority';

const buttonVariants = cva(
  'inline-flex items-center justify-center whitespace-nowrap rounded-md text-body font-medium transition-all duration-200 ease-out focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 active:scale-[0.97]',
  {
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground hover:brightness-110',
        destructive: 'bg-destructive text-destructive-foreground hover:brightness-110',
        outline:
          'border border-input bg-background hover:bg-accent hover:text-accent-foreground hover:border-primary/40',
        secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80',
        ghost: 'hover:bg-accent hover:text-accent-foreground',
        link: 'text-primary underline-offset-4 hover:underline',
      },
      size: {
        default: 'h-11 px-5 py-2.5',
        sm: 'h-9 max-md:h-11 rounded-md px-3.5 text-sm',
        lg: 'h-12 rounded-md px-8 text-base',
        icon: 'h-10 w-10 max-md:h-11 max-md:w-11',
        // Refresh scale — primary CTA / row action / chip.
        xl: 'h-42px max-md:h-11 gap-2 rounded-md px-4 text-meta',
        chip: 'h-8 max-md:h-11 gap-1.5 rounded-md px-3.5 text-label',
        xs: 'h-30px max-md:h-11 gap-1.5 rounded-md px-3 text-label',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  },
);

export interface ButtonProps
  extends React.ComponentProps<'button'>, VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

export function Button({ ref, className, variant, size, asChild = false, ...props }: ButtonProps) {
  const Comp = asChild ? Slot : 'button';
  return <Comp ref={ref} className={cn(buttonVariants({ variant, size, className }))} {...props} />;
}
