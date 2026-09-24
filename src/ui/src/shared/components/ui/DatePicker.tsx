import * as PopoverPrimitive from '@radix-ui/react-popover';
import { cn } from '@shared/lib/utils';
import { format, isValid, parseISO } from 'date-fns';
import { CalendarIcon } from 'lucide-react';

import { Calendar } from './Calendar';

import type { Matcher } from 'react-day-picker';

const DEFAULT_START_YEAR = new Date().getFullYear() - 100;
const DEFAULT_END_YEAR = new Date().getFullYear() + 10;

export interface DatePickerProps {
  /** ISO date string (YYYY-MM-DD) or null/undefined for no selection */
  value: string | null | undefined;
  onChange: (value: string | null) => void;
  placeholder?: string;
  className?: string;
  disabled?: boolean;
  /** Disable days before this date. */
  minDate?: Date;
  /** Disable days after this date. */
  maxDate?: Date;
  /** Applied as data-testid on the trigger button for E2E tests. */
  testId?: string;
}

function parseDate(value: string | null | undefined): Date | undefined {
  if (!value) return undefined;
  const d = parseISO(value);
  return isValid(d) ? d : undefined;
}

export function DatePicker({
  value,
  onChange,
  placeholder = 'Pick a date',
  className,
  disabled,
  minDate,
  maxDate,
  testId,
}: DatePickerProps) {
  const selected = parseDate(value);

  function handleSelect(date: Date | undefined) {
    onChange(date ? format(date, 'yyyy-MM-dd') : null);
  }

  const disabledMatchers: Matcher[] = [
    ...(minDate ? [{ before: minDate }] : []),
    ...(maxDate ? [{ after: maxDate }] : []),
  ];

  return (
    <PopoverPrimitive.Root>
      <PopoverPrimitive.Trigger asChild>
        <button
          type="button"
          disabled={disabled}
          data-testid={testId}
          data-value={value ?? ''}
          className={cn(
            'bg-background border-input outline-ring flex h-10 w-full items-center justify-between rounded-md border px-3 py-2 text-sm',
            'focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none',
            'disabled:cursor-not-allowed disabled:opacity-50',
            selected ? 'text-foreground' : 'text-muted-foreground',
            className,
          )}
        >
          <span>{selected ? format(selected, 'dd MMM yyyy') : placeholder}</span>
          <CalendarIcon className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </button>
      </PopoverPrimitive.Trigger>

      <PopoverPrimitive.Portal>
        <PopoverPrimitive.Content
          align="start"
          sideOffset={4}
          className={cn(
            'bg-popover text-popover-foreground border-border z-50 rounded-md border shadow-md',
            'data-[state=open]:animate-in data-[state=closed]:animate-out',
            'data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0',
            'data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95',
            'data-[side=bottom]:slide-in-from-top-2 data-[side=top]:slide-in-from-bottom-2',
          )}
        >
          <Calendar
            mode="single"
            captionLayout="dropdown"
            hideNavigation
            startMonth={new Date(DEFAULT_START_YEAR, 0)}
            endMonth={new Date(DEFAULT_END_YEAR, 11)}
            selected={selected}
            onSelect={handleSelect}
            disabled={disabledMatchers.length > 0 ? disabledMatchers : disabled}
            classNames={{
              month_caption: 'flex items-center justify-center gap-1 py-1',
              caption_label: 'hidden',
            }}
          />
        </PopoverPrimitive.Content>
      </PopoverPrimitive.Portal>
    </PopoverPrimitive.Root>
  );
}
