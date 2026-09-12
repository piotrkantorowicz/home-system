import { render } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ProtectedRoute } from './ProtectedRoute';

const auth = vi.hoisted(() => ({
  isLoading: false,
  isAuthenticated: false,
  activeNavigator: undefined as string | undefined,
  signinRedirect: vi.fn(),
}));

vi.mock('react-oidc-context', () => ({ useAuth: () => auth }));

describe('ProtectedRoute', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
    auth.activeNavigator = undefined;
  });

  it('starts login for an unauthenticated visitor', () => {
    render(<ProtectedRoute>Private content</ProtectedRoute>);

    expect(auth.signinRedirect).toHaveBeenCalledOnce();
  });

  it('does not start login when token revocation clears loading during logout', () => {
    auth.activeNavigator = 'signoutRedirect';

    render(<ProtectedRoute>Private content</ProtectedRoute>);

    expect(auth.signinRedirect).not.toHaveBeenCalled();
    expect(sessionStorage.getItem('returnUrl')).toBeNull();
  });
});
