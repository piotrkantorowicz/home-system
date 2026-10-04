import { ChevronLeft, ChevronRight } from 'lucide-react';

import { Button } from './Button';

export interface MonthStepperProps {
  /** Already-formatted month, e.g. "October 2026". */
  label: string;
  groupLabel: string;
  previousLabel: string;
  nextLabel: string;
  onPrevious: () => void;
  onNext: () => void;
}

/** ‹ October 2026 › — shared by every month-scoped screen so the control never moves. */
export function MonthStepper({
  label,
  groupLabel,
  previousLabel,
  nextLabel,
  onPrevious,
  onNext,
}: MonthStepperProps) {
  return (
    <div role="group" aria-label={groupLabel} className="flex items-center gap-1">
      <Button variant="outline" size="icon" aria-label={previousLabel} onClick={onPrevious}>
        <ChevronLeft className="size-4" />
      </Button>
      <p className="min-w-40 text-center font-semibold capitalize" aria-live="polite">
        {label}
      </p>
      <Button variant="outline" size="icon" aria-label={nextLabel} onClick={onNext}>
        <ChevronRight className="size-4" />
      </Button>
    </div>
  );
}
