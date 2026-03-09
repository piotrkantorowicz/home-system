import { useEffect } from 'react';
import { userManager } from './userManager';

export function SilentRenew() {
  useEffect(() => {
    // Complete the silent renewal callback using the shared UserManager.
    // This ensures the renewed token is stored in the same instance
    // that AuthProvider and the API client use.
    userManager.signinSilentCallback().catch((error) => {
      console.error('Silent renew callback error:', error);
    });
  }, []);

  return null;
}
