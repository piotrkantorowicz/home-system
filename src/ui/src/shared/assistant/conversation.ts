const STORAGE_PREFIX = 'home-system-assistant:';

/** The server's history allowance (`Assistant:MaxHistoryTokens` 20 000 × ~4 characters per token). */
const MAX_STORED_HISTORY_CHARS = 80_000;

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

/**
 * Keeps the newest turns whose history fits `maxChars` of JSON, cutting only before a user message
 * so a tool call is never separated from its result (as the server's `Recent` does). `entries` keep
 * the same number of user turns. At least the latest turn is kept.
 */
export function recentTurns(conversation: Conversation, maxChars: number): Conversation {
  const { history, entries } = conversation;
  let size = 0;
  let start = history.length;
  for (let i = history.length - 1; i >= 0; i--) {
    size += JSON.stringify(history[i]).length;
    const isTurnStart = (history[i] as { role?: unknown } | null)?.role === 'user';
    if (size > maxChars && start < history.length) break;
    if (isTurnStart) start = i;
  }

  const kept = history.slice(start);
  const turns = kept.filter((m) => (m as { role?: unknown } | null)?.role === 'user').length;
  let entryStart = entries.length;
  for (let seen = 0; entryStart > 0 && seen < Math.max(turns, 1);) {
    entryStart--;
    if (entries[entryStart]?.role === 'user') seen++;
  }
  return { history: kept, entries: entries.slice(entryStart) };
}

export function saveConversation(subject: string, conversation: Conversation): void {
  // Older turns are dropped by the server anyway, so storing them only fills the quota.
  for (const maxChars of [MAX_STORED_HISTORY_CHARS, MAX_STORED_HISTORY_CHARS / 2]) {
    try {
      const recent = recentTurns(conversation, maxChars);
      window.localStorage.setItem(STORAGE_PREFIX + subject, JSON.stringify(recent));
      return;
    } catch {
      // storage unavailable or full — retry once smaller, else it lives until reload
    }
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
