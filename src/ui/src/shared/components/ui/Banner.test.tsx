import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { Banner } from './Banner';

describe('Banner', () => {
  it('uses role=alert for the error variant and role=status otherwise', () => {
    const { rerender } = render(<Banner variant="error">boom</Banner>);
    expect(screen.getByRole('alert')).toHaveTextContent('boom');

    rerender(<Banner variant="success">ok</Banner>);
    expect(screen.getByRole('status')).toHaveTextContent('ok');
  });

  it('renders a retry button that fires the callback', async () => {
    const onRetry = vi.fn();
    render(
      <Banner variant="error" title="Failed" onRetry={onRetry} retryLabel="Try again">
        details
      </Banner>,
    );
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }));
    expect(onRetry).toHaveBeenCalledOnce();
  });
});
