import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { Sheet, SheetContent } from './Sheet';

describe('Sheet', () => {
  it('renders children when open', () => {
    render(
      <Sheet open>
        <SheetContent onClose={vi.fn()}>
          <p>Drawer content</p>
        </SheetContent>
      </Sheet>,
    );

    expect(screen.getByText('Drawer content')).toBeInTheDocument();
  });

  it('does not show content when closed', () => {
    render(
      <Sheet open={false}>
        <SheetContent onClose={vi.fn()}>
          <p>Hidden content</p>
        </SheetContent>
      </Sheet>,
    );

    expect(screen.queryByText('Hidden content')).not.toBeInTheDocument();
  });

  it('calls onClose when the close button is clicked', async () => {
    const onClose = vi.fn();
    render(
      <Sheet open>
        <SheetContent onClose={onClose}>
          <p>Content</p>
        </SheetContent>
      </Sheet>,
    );

    await userEvent.click(screen.getByRole('button', { name: /close/i }));

    expect(onClose).toHaveBeenCalledOnce();
  });
});
