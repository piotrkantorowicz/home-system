import { createContext, use } from 'react';

type NavigationRestriction = { allowedPath: string; reason: string } | null;

export const NavigationAccessContext = createContext<NavigationRestriction>(null);

export function useNavigationAccess() {
  const restriction = use(NavigationAccessContext);
  return {
    reason: restriction?.reason,
    canNavigate: (path: string) => !restriction || path === restriction.allowedPath,
  };
}
