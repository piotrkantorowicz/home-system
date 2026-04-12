import * as PopoverPrimitive from '@radix-ui/react-popover';
import { cn } from '@shared/lib/utils';
import { format, isValid, parseISO } from 'date-fns';
import { CalendarIcon } from 'lucide-react';

import type { Matcher } from 'react-day-picker';

import { Calendar } from './Calendar';

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
          className={cn(
            'flex h-10 w-full items-center justify-between rounded-md border px-3 py-2 text-sm',
            'focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none',
            'disabled:cursor-not-allowed disabled:opacity-50',
            !selected && 'text-muted-foreground',
            className,
          )}
          style={{
            backgroundColor: 'hsl(var(--color-background))',
            borderColor: 'hsl(var(--color-input))',
            color: selected ? 'hsl(var(--color-foreground))' : 'hsl(var(--color-muted-foreground))',
            outlineColor: 'hsl(var(--color-ring))',
          }}
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
            'z-50 rounded-md border shadow-md',
            'data-[state=open]:animate-in data-[state=closed]:animate-out',
            'data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0',
            'data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95',
            'data-[side=bottom]:slide-in-from-top-2 data-[side=top]:slide-in-from-bottom-2',
          )}
          style={{
            backgroundColor: 'hsl(var(--color-popover))',
            color: 'hsl(var(--color-popover-foreground))',
            borderColor: 'hsl(var(--color-border))',
          }}
        >
          <Calendar
            mode="single"
            captionLayout="dropdown"
            startMonth={new Date(DEFAULT_START_YEAR, 0)}
            endMonth={new Date(DEFAULT_END_YEAR, 11)}
            selected={selected}
            onSelect={handleSelect}
            disabled={disabledMatchers.length > 0 ? disabledMatchers : disabled}
          />
        </PopoverPrimitive.Content>
      </PopoverPrimitive.Portal>
    </PopoverPrimitive.Root>
  );
}
