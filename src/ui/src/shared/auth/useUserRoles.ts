import { useAuth } from 'react-oidc-context';

/** Values of the signed-in user's `roles` claim (Authentik `roles` scope); empty when absent. */
export function useUserRoles(): readonly string[] {
  const roles: unknown = useAuth().user?.profile.roles;
  return Array.isArray(roles) ? roles.filter((r): r is string => typeof r === 'string') : [];
}
