import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5, // 5 minutes
      retry: (failureCount, error) => {
        // Don't retry on 4xx errors
        if (typeof error === 'object' && 'status' in error) {
          const status = error.status as number;
          if (status >= 400 && status < 500) return false;
        }
        return failureCount < 3;
      },
      refetchOnWindowFocus: false,
    },
    mutations: {
      // Per-mutation onError handlers render user-facing toasts via the
      // useToast React context, which can't be called outside a component.
      // The global hook is limited to logging so errors are captured even
      // when a call site forgets its own onError.
      onError: (error) => {
        console.error('Mutation error:', error);
      },
    },
  },
});
