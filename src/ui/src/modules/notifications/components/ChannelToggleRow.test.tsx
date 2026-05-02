import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Bell } from 'lucide-react';
import { describe, it, expect, vi } from 'vitest';

import { ChannelToggleRow } from './ChannelToggleRow';

describe('ChannelToggleRow', () => {
  it('renders the label and description', () => {
    render(
      <ChannelToggleRow
        id="console"
        label="In-app"
        description="Show notifications inside this app."
        icon={Bell}
        checked={true}
        onChange={vi.fn()}
      />,
    );

    expect(screen.getByText('In-app')).toBeInTheDocument();
    expect(screen.getByText('Show notifications inside this app.')).toBeInTheDocument();
  });

  it('calls onChange with the new value when the toggle is clicked', async () => {
    const onChange = vi.fn();

    render(
      <ChannelToggleRow
        id="console"
        label="In-app"
        description="Show notifications inside this app."
        icon={Bell}
        checked={false}
        onChange={onChange}
      />,
    );

    await userEvent.click(screen.getByRole('switch', { name: 'In-app' }));

    expect(onChange).toHaveBeenCalledOnce();
    expect(onChange).toHaveBeenCalledWith(true);
  });

  it('renders with aria-disabled and shows disabledReason when disabled', () => {
    render(
      <ChannelToggleRow
        id="email"
        label="Email"
        description="Send notifications to your email address."
        icon={Bell}
        checked={false}
        disabled={true}
        disabledReason="Coming soon"
        onChange={vi.fn()}
      />,
    );

    const toggle = screen.getByRole('switch', { name: 'Email' });
    expect(toggle).toHaveAttribute('aria-disabled', 'true');
    expect(toggle).toBeDisabled();
    expect(screen.getByText('Coming soon')).toBeInTheDocument();
  });
});
