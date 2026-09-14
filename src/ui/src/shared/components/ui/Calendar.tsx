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
        month_caption: 'flex items-center justify-between gap-2 px-1 pt-1 h-9',
        caption_label: 'text-sm font-medium',
        nav: 'flex items-center gap-1',
        button_previous:
          'inline-flex h-7 w-7 items-center justify-center rounded-md border p-0 opacity-50 shadow-sm transition-colors hover:opacity-100 focus-visible:outline-none focus-visible:ring-1',
        button_next:
          'inline-flex h-7 w-7 items-center justify-center rounded-md border p-0 opacity-50 shadow-sm transition-colors hover:opacity-100 focus-visible:outline-none focus-visible:ring-1',
        dropdowns: 'flex flex-1 items-center justify-center gap-1',
        dropdown:
          'relative inline-flex items-center rounded-md border px-2 py-1 text-sm font-medium focus-within:ring-1 focus-within:outline-none',
        months_dropdown: 'h-7 appearance-none bg-transparent pr-4 focus:outline-none',
        years_dropdown: 'h-7 appearance-none bg-transparent pr-4 focus:outline-none',
        month_grid: 'w-full border-collapse',
        weekdays: 'flex',
        weekday: 'text-muted-foreground w-9 rounded-md text-center text-[0.8rem] font-normal',
        week: 'mt-2 flex w-full',
        day: 'relative p-0 text-center text-sm',
        day_button:
          'h-9 w-9 rounded-md p-0 font-normal transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:outline-none focus-visible:ring-1 aria-selected:opacity-100',
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
