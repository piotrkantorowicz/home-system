import { useEffect } from 'react';
import type { ReactNode } from 'react';
import { useAuth } from 'react-oidc-context';

interface ProtectedRouteProps {
  children: ReactNode;
  fallback?: ReactNode;
}

export function ProtectedRoute({ children, fallback }: ProtectedRouteProps) {
  const auth = useAuth();

  useEffect(() => {
    if (!auth.isLoading && !auth.isAuthenticated) {
      // Only save if not already saved (the 401 handler may have saved a better URL)
      if (!sessionStorage.getItem('returnUrl')) {
        const pathname = window.location.pathname;
        if (pathname !== '/' && pathname.startsWith('/') && !pathname.startsWith('//')) {
          sessionStorage.setItem('returnUrl', pathname);
        }
      }
      auth.signinRedirect();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [auth.isLoading, auth.isAuthenticated]);

  if (auth.isLoading) {
    return (
      fallback || (
        <div className="flex min-h-screen items-center justify-center">
          <div className="text-center">
            <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-current border-r-transparent motion-reduce:animate-[spin_1.5s_linear_infinite] mb-4" />
            <p className="text-muted-foreground">Loading...</p>
          </div>
        </div>
      )
    );
  }

  if (auth.error) {
    return (
      <div className="flex min-h-screen items-center justify-center p-8">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-destructive mb-2">Authentication Error</h1>
          <p className="text-muted-foreground">{auth.error.message}</p>
        </div>
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    return fallback || <div>Redirecting to login...</div>;
  }

  return <>{children}</>;
}
