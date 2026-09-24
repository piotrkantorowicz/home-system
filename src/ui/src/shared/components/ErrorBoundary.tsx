import { ErrorBoundary as ReactErrorBoundary } from 'react-error-boundary';

import type { ReactNode } from 'react';
import type { FallbackProps } from 'react-error-boundary';

function ErrorFallback({ error, resetErrorBoundary }: FallbackProps) {
  const errorMessage = error instanceof Error ? error.message : 'An unexpected error occurred.';
  const errorStack = error instanceof Error ? error.stack : undefined;
  return (
    <div className="ambient-bg flex min-h-screen items-center justify-center p-8">
      <div className="animate-scale-in max-w-md text-center">
        <div className="bg-destructive/10 mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-2xl">
          <svg
            className="text-destructive h-8 w-8"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.07 16.5c-.77.833.192 2.5 1.732 2.5z"
            />
          </svg>
        </div>
        <h1 className="mb-3 text-2xl font-bold">Something went wrong</h1>
        <p className="text-muted-foreground text-0-95rem mb-6">{errorMessage}</p>
        {import.meta.env.DEV && errorStack && (
          <pre className="bg-muted/50 text-muted-foreground mb-6 max-h-48 overflow-auto rounded-xl border p-4 text-left text-xs">
            {errorStack}
          </pre>
        )}
        <button
          onClick={resetErrorBoundary}
          className="bg-primary text-primary-foreground shadow-primary/20 rounded-lg px-6 py-2.5 text-sm font-medium shadow-md transition-all duration-200 hover:-translate-y-0.5 hover:brightness-110"
        >
          Try again
        </button>
      </div>
    </div>
  );
}

interface AppErrorBoundaryProps {
  children: ReactNode;
}

export function AppErrorBoundary({ children }: AppErrorBoundaryProps) {
  return <ReactErrorBoundary FallbackComponent={ErrorFallback}>{children}</ReactErrorBoundary>;
}
