import { useHousehold } from '@modules/household';
import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';
import { useAuth } from 'react-oidc-context';

import { budgetQueryKeys } from '../api/queryKeys';

import type { ReactNode } from 'react';

/**
 * Drops every cached Budget response when the signed-in person or household changes, and when the
 * user leaves Budget — financial rows are never kept around for someone else or for later. Query
 * keys carry the same identity, so a stale row can't be read even before the cleanup runs.
 */
export function BudgetCacheBoundary({ children }: { children: ReactNode }) {
  const client = useQueryClient();
  const subject = useAuth().user?.profile.sub;
  const householdId = useHousehold().household?.id;

  useEffect(() => {
    // Rows cached for any other person or household are gone as soon as the identity changes…
    client.removeQueries({
      queryKey: budgetQueryKeys.all(),
      predicate: (query) => query.queryKey[2] !== subject || query.queryKey[3] !== householdId,
    });
    // …and everything goes when the user leaves Budget or this identity ends.
    return () => {
      client.removeQueries({ queryKey: budgetQueryKeys.all() });
    };
  }, [client, subject, householdId]);

  return children;
}
