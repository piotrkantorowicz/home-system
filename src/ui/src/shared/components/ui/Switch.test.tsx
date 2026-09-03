import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { Switch } from './Switch';

describe('Switch', () => {
  it('exposes the checked state through role and aria', () => {
    render(<Switch checked onCheckedChange={vi.fn()} aria-label="Meal reminders" />);
    const sw = screen.getByRole('switch', { name: 'Meal reminders' });
    expect(sw).toBeChecked();
  });

  it('toggles on click', async () => {
    const onCheckedChange = vi.fn();
    render(<Switch checked={false} onCheckedChange={onCheckedChange} aria-label="x" />);
    await userEvent.click(screen.getByRole('switch'));
    expect(onCheckedChange).toHaveBeenCalledWith(true);
  });

  it('does not fire when disabled', async () => {
    const onCheckedChange = vi.fn();
    render(<Switch checked={false} onCheckedChange={onCheckedChange} disabled aria-label="x" />);
    await userEvent.click(screen.getByRole('switch'));
    expect(onCheckedChange).not.toHaveBeenCalled();
  });
});
