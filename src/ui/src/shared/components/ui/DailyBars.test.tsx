import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { DailyBars, type DailyBar } from './DailyBars';

const bar = (key: string, value: number | null, extra: Partial<DailyBar> = {}): DailyBar => ({
  key,
  label: 'Mon',
  sublabel: '1.10',
  value,
  text: String(value),
  srText: `${key}: ${String(value)}`,
  ...extra,
});

const props = { overLabel: 'over target', missingText: '—', ariaLabel: 'Last 7 days' };

describe('DailyBars', () => {
  it('shows value labels, a ▲ marker for over-target days and a screen-reader list', () => {
    render(
      <DailyBars
        {...props}
        target={2000}
        targetLabel="2 000 target"
        days={[bar('a', 1800), bar('b', 2300, { over: true })]}
      />,
    );
    expect(screen.getByText('▲ 2300')).toBeInTheDocument();
    expect(screen.getByText('2 000 target')).toBeInTheDocument();
    expect(screen.getByTestId('target-line')).toBeInTheDocument();
    expect(screen.getByText('b: 2300 (over target)')).toBeInTheDocument();
  });

  it('renders a missing day differently from a logged zero and omits the line without a target', () => {
    render(<DailyBars {...props} days={[bar('a', 0), bar('b', null, { srText: 'b: none' })]} />);
    expect(screen.getByText('0')).toBeInTheDocument();
    expect(screen.getByText('—')).toBeInTheDocument();
    expect(screen.getByText('b: none')).toBeInTheDocument();
    expect(screen.queryByTestId('target-line')).toBeNull();
  });
});
