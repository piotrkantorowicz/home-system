import { useRef } from 'react';

/**
 * One idempotency key per logical submission. Retrying the same payload (e.g. after a network
 * error) reuses the key so the server recognises the retry; changing the payload starts a new
 * logical submission with a new key. Call `reset` after a success.
 */
export function useRequestId() {
  const current = useRef<{ signature: string; id: string } | null>(null);
  return {
    idFor(signature: string): string {
      if (current.current?.signature !== signature)
        current.current = { signature, id: crypto.randomUUID() };
      return current.current.id;
    },
    reset() {
      current.current = null;
    },
  };
}
