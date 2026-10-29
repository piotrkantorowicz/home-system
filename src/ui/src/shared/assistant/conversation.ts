const STORAGE_PREFIX = 'home-system-assistant:';

export interface ToolActivity {
  name: string;
  summary: string;
  /** `null` while the tool runs. */
  ok: boolean | null;
}

/** A write the server holds until the user confirms or cancels it on its card (design §6). */
export interface PendingAction {
  /** Signed by the server; Confirm and Cancel send it back unchanged. */
  token: string;
  name: string;
  arguments: Record<string, unknown>;
  destructive: boolean;
  status: 'waiting' | 'confirmed' | 'cancelled';
}

export type AssistantEntry =
  | { role: 'user'; text: string }
  | {
      role: 'assistant';
      text: string;
      tools: ToolActivity[];
      error?: string;
      pending?: PendingAction;
    };

/**
 * One user's conversation on this device (design §4). `history` is what the server's `history`
 * events returned, sent back verbatim on the next request; `entries` is what the panel shows.
 */
export interface Conversation {
  history: unknown[];
  entries: AssistantEntry[];
}

export const EMPTY_CONVERSATION: Conversation = { history: [], entries: [] };

export function loadConversation(subject: string): Conversation {
  try {
    const raw = window.localStorage.getItem(STORAGE_PREFIX + subject);
    if (!raw) return EMPTY_CONVERSATION;
    const parsed = JSON.parse(raw) as Partial<Conversation>;
    return Array.isArray(parsed.history) && Array.isArray(parsed.entries)
      ? { history: parsed.history, entries: parsed.entries }
      : EMPTY_CONVERSATION;
  } catch {
    return EMPTY_CONVERSATION;
  }
}

export function saveConversation(subject: string, conversation: Conversation): void {
  try {
    window.localStorage.setItem(STORAGE_PREFIX + subject, JSON.stringify(conversation));
  } catch {
    // storage unavailable or full — the conversation lives until reload
  }
}

export function clearConversation(subject: string): void {
  try {
    window.localStorage.removeItem(STORAGE_PREFIX + subject);
  } catch {
    // storage unavailable — nothing was saved
  }
}

/** Sign-out: the next person on this device never sees, or sends to the model, this history. */
export function clearAllConversations(): void {
  try {
    for (const key of Object.keys(window.localStorage)) {
      if (key.startsWith(STORAGE_PREFIX)) window.localStorage.removeItem(key);
    }
  } catch {
    // storage unavailable — nothing was saved
  }
}
