import { useEffect } from 'react';
import { AuthProvider as OidcAuthProvider } from 'react-oidc-context';

import { userManager } from './userManager';

import type { ReactNode } from 'react';

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const onSigninCallback = () => {
    // Remove the code and state from the URL after successful login
    window.history.replaceState({}, document.title, window.location.pathname);
  };

  // Listen for silent renew errors on the shared UserManager
  useEffect(() => {
    const handleSilentRenewError = (error: Error) => {
      console.warn('Silent token renewal failed:', error.message);
      // Don't force redirect here — let ProtectedRoute handle it
      // when the token actually expires and isAuthenticated flips to false.
    };

    userManager.events.addSilentRenewError(handleSilentRenewError);
    return () => {
      userManager.events.removeSilentRenewError(handleSilentRenewError);
    };
  }, []);

  return (
    <OidcAuthProvider userManager={userManager} onSigninCallback={onSigninCallback}>
      {children}
    </OidcAuthProvider>
  );
}
