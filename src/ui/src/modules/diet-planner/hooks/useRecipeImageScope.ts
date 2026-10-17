import { useHousehold } from '@modules/household';
import { useAuth } from 'react-oidc-context';

export function useRecipeImageScope() {
  const auth = useAuth();
  const { household, myPersonId, myRole, isLoading, isError } = useHousehold();
  const subject = auth.user?.profile.sub;
  if (!auth.isAuthenticated || !subject || isLoading || isError) return null;
  return JSON.stringify([subject, household?.id, myPersonId, myRole]);
}
