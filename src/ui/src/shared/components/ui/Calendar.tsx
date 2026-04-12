import { cn } from '@shared/lib/utils';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { DayPicker, type DayPickerProps } from 'react-day-picker';

export type CalendarProps = DayPickerProps;

export function Calendar({
  className,
  classNames,
  showOutsideDays = true,
  ...props
}: CalendarProps) {
  return (
    <DayPicker
      showOutsideDays={showOutsideDays}
      className={cn('p-3', className)}
      classNames={{
        months: 'flex flex-col sm:flex-row gap-4',
        month: 'flex flex-col gap-4',
        month_caption: 'flex justify-center items-center pt-1 relative h-7',
        caption_label: 'text-sm font-medium',
        nav: 'absolute inset-x-0 top-0 flex items-center justify-between px-1',
        button_previous:
          'inline-flex h-7 w-7 items-center justify-center rounded-md border border-input bg-background p-0 opacity-50 shadow-sm transition-colors hover:bg-accent hover:opacity-100 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring',
        button_next:
          'inline-flex h-7 w-7 items-center justify-center rounded-md border border-input bg-background p-0 opacity-50 shadow-sm transition-colors hover:bg-accent hover:opacity-100 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring',
        month_grid: 'w-full border-collapse',
        weekdays: 'flex',
        weekday: 'text-muted-foreground w-9 rounded-md text-center text-[0.8rem] font-normal',
        week: 'mt-2 flex w-full',
        day: 'relative p-0 text-center text-sm',
        day_button:
          'h-9 w-9 rounded-md p-0 font-normal transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring aria-selected:opacity-100',
        selected:
          '[&>button]:bg-primary [&>button]:text-primary-foreground [&>button]:hover:bg-primary [&>button]:hover:text-primary-foreground',
        today: '[&>button]:bg-accent [&>button]:text-accent-foreground',
        outside: '[&>button]:text-muted-foreground [&>button]:opacity-50',
        disabled:
          '[&>button]:text-muted-foreground [&>button]:opacity-50 [&>button]:cursor-not-allowed',
        range_middle:
          '[&>button]:rounded-none [&>button]:bg-accent [&>button]:text-accent-foreground',
        range_start: '[&>button]:rounded-l-md',
        range_end: '[&>button]:rounded-r-md',
        hidden: 'invisible',
        ...classNames,
      }}
      components={{
        Chevron: ({ orientation }) =>
          orientation === 'left' ? (
            <ChevronLeft className="h-4 w-4" />
          ) : (
            <ChevronRight className="h-4 w-4" />
          ),
      }}
      {...props}
    />
  );
}
