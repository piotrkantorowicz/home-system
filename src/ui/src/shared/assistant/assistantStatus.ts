import { queryOptions, useQuery } from '@tanstack/react-query';

import { assistantFetch } from './client';

export interface AssistantStatus {
  provider: string;
  /** The model accepts image attachments. */
  images: boolean;
}

/** `null` when the assistant is switched off (the endpoint answers 404); the panel then hides. */
export function assistantStatusOptions() {
  return queryOptions({
    queryKey: ['assistant', 'status'] as const,
    queryFn: async ({ signal }): Promise<AssistantStatus | null> => {
      const response = await assistantFetch('', { signal });
      if (response.status === 404) return null;
      if (!response.ok) throw new Error(`Assistant status failed (${String(response.status)})`);
      return (await response.json()) as AssistantStatus;
    },
    staleTime: Infinity,
    retry: false,
  });
}

export function useAssistantStatus() {
  return useQuery(assistantStatusOptions());
}
