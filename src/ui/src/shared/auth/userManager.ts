import { UserManager } from 'oidc-client-ts';
import { oidcConfig } from './authConfig';

// Shared UserManager instance used by both React (AuthProvider) and the API client.
// This ensures signinSilent() from the API interceptor uses the same state
// as the AuthProvider, preventing the "two managers" bug.
export const userManager = new UserManager(oidcConfig);
