import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { SegmentedControl } from './SegmentedControl';

const options = [
  { value: 'day', label: 'Day' },
  { value: 'week', label: 'Week' },
  { value: 'month', label: 'Month' },
] as const;

describe('SegmentedControl', () => {
  it('marks the selected option as checked', () => {
    render(
      <SegmentedControl options={[...options]} value="week" onChange={vi.fn()} label="View mode" />,
    );
    expect(screen.getByRole('radio', { name: 'Week' })).toBeChecked();
    expect(screen.getByRole('radio', { name: 'Day' })).not.toBeChecked();
  });

  it('calls onChange when another option is clicked', async () => {
    const onChange = vi.fn();
    render(
      <SegmentedControl
        options={[...options]}
        value="week"
        onChange={onChange}
        label="View mode"
      />,
    );
    await userEvent.click(screen.getByRole('radio', { name: 'Month' }));
    expect(onChange).toHaveBeenCalledWith('month');
  });

  it('moves selection with the arrow keys', async () => {
    const onChange = vi.fn();
    render(
      <SegmentedControl
        options={[...options]}
        value="week"
        onChange={onChange}
        label="View mode"
      />,
    );
    screen.getByRole('radio', { name: 'Week' }).focus();
    await userEvent.keyboard('{ArrowRight}');
    expect(onChange).toHaveBeenCalledWith('month');
  });
});
