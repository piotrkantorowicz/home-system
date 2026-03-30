import { render, screen } from '@testing-library/react';

import { AppErrorBoundary } from './ErrorBoundary';

function ThrowingComponent({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) {
    throw new Error('Test error message');
  }
  return <div>Content loaded</div>;
}

describe('AppErrorBoundary', () => {
  // Suppress console.error for expected errors in tests
  beforeEach(() => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders children when no error', () => {
    render(
      <AppErrorBoundary>
        <ThrowingComponent shouldThrow={false} />
      </AppErrorBoundary>,
    );

    expect(screen.getByText('Content loaded')).toBeInTheDocument();
  });

  it('renders error fallback when child throws', () => {
    render(
      <AppErrorBoundary>
        <ThrowingComponent shouldThrow={true} />
      </AppErrorBoundary>,
    );

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
    // The error message appears in a <p> element (and optionally in a <pre> stack trace in DEV)
    const errorMessages = screen.getAllByText(/Test error message/);
    expect(errorMessages.length).toBeGreaterThan(0);
  });

  it('shows retry button in error fallback', () => {
    render(
      <AppErrorBoundary>
        <ThrowingComponent shouldThrow={true} />
      </AppErrorBoundary>,
    );

    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
  });
});
