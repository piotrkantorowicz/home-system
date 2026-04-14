import { render, screen, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

import { ToastContainer } from './Toast';

import type { ToastItem } from '@shared/context/ToastContext';

function makeToast(overrides: Partial<ToastItem> = {}): ToastItem {
  return {
    id: 'toast-1',
    variant: 'success',
    message: 'Test message',
    duration: 0, // 0 = no auto-dismiss in tests
    ...overrides,
  };
}

describe('ToastContainer', () => {
  it('renders nothing when the toasts list is empty', () => {
    const { container } = render(<ToastContainer toasts={[]} onDismiss={vi.fn()} />);
    // createPortal renders into document.body, not the container div
    expect(document.body.querySelector('[aria-label="Notifications"]')).toBeNull();
    expect(container).toBeEmptyDOMElement();
  });

  it('renders the notification region when there are toasts', () => {
    render(<ToastContainer toasts={[makeToast()]} onDismiss={vi.fn()} />);

    expect(document.body.querySelector('[aria-label="Notifications"]')).toBeInTheDocument();
  });

  it('renders the toast message', () => {
    render(<ToastContainer toasts={[makeToast({ message: 'Hello world' })]} onDismiss={vi.fn()} />);

    expect(screen.getByText('Hello world')).toBeInTheDocument();
  });

  it('renders multiple toasts', () => {
    const toasts = [
      makeToast({ id: 'a', message: 'First' }),
      makeToast({ id: 'b', message: 'Second' }),
    ];

    render(<ToastContainer toasts={toasts} onDismiss={vi.fn()} />);

    expect(screen.getByText('First')).toBeInTheDocument();
    expect(screen.getByText('Second')).toBeInTheDocument();
  });

  it('calls onDismiss when the close button is clicked', async () => {
    const onDismiss = vi.fn();
    render(<ToastContainer toasts={[makeToast({ id: 'toast-99' })]} onDismiss={onDismiss} />);

    await userEvent.click(screen.getByRole('button', { name: 'Dismiss notification' }));

    // onDismiss is called after the 250ms exit animation
    await act(async () => {
      await new Promise((r) => setTimeout(r, 300));
    });

    expect(onDismiss).toHaveBeenCalledOnce();
    expect(onDismiss).toHaveBeenCalledWith('toast-99');
  });
});

describe('ToastContainer — variant a11y roles', () => {
  it.each([
    ['success', 'status'],
    ['info', 'status'],
    ['error', 'alert'],
    ['warning', 'alert'],
  ] as const)('%s toast has role="%s"', (variant, expectedRole) => {
    render(<ToastContainer toasts={[makeToast({ variant })]} onDismiss={vi.fn()} />);

    expect(screen.getByRole(expectedRole)).toBeInTheDocument();
  });
});

describe('ToastContainer — auto-dismiss', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('calls onDismiss after the specified duration', () => {
    const onDismiss = vi.fn();
    render(
      <ToastContainer
        toasts={[makeToast({ id: 'timed', duration: 1000 })]}
        onDismiss={onDismiss}
      />,
    );

    // Not called before the duration elapses
    expect(onDismiss).not.toHaveBeenCalled();

    // Advance past duration + exit animation (250 ms)
    act(() => {
      vi.advanceTimersByTime(1300);
    });

    expect(onDismiss).toHaveBeenCalledOnce();
    expect(onDismiss).toHaveBeenCalledWith('timed');
  });

  it('does not call onDismiss when duration is 0', () => {
    const onDismiss = vi.fn();
    render(
      <ToastContainer toasts={[makeToast({ id: 'no-auto', duration: 0 })]} onDismiss={onDismiss} />,
    );

    act(() => {
      vi.advanceTimersByTime(10_000);
    });

    expect(onDismiss).not.toHaveBeenCalled();
  });
});
