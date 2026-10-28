import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { assistantFetch } from './client';
import {
  clearConversation,
  EMPTY_CONVERSATION,
  loadConversation,
  saveConversation,
} from './conversation';
import { readEvents } from './readEvents';

import type { AssistantEntry, Conversation, ToolActivity } from './conversation';
import type { ServerEvent } from './readEvents';

type AssistantReply = Extract<AssistantEntry, { role: 'assistant' }>;

export interface AssistantChat {
  entries: AssistantEntry[];
  streaming: boolean;
  send: (message: string) => Promise<void>;
  clear: () => void;
}

function field(data: unknown, name: string): unknown {
  return typeof data === 'object' && data !== null
    ? (data as Record<string, unknown>)[name]
    : undefined;
}

function text(data: unknown, name: string): string {
  const value = field(data, name);
  return typeof value === 'string' ? value : '';
}

function applyEvent(reply: AssistantReply, event: ServerEvent): AssistantReply {
  switch (event.type) {
    case 'text':
      return { ...reply, text: reply.text + text(event.data, 'text') };
    case 'tool_call': {
      const tool: ToolActivity = {
        name: text(event.data, 'name'),
        summary: text(event.data, 'summary'),
        ok: null,
      };
      return { ...reply, tools: [...reply.tools, tool] };
    }
    case 'tool_result': {
      // The result belongs to the latest call of that tool still running.
      const name = text(event.data, 'name');
      let index = reply.tools.length - 1;
      while (index >= 0 && !(reply.tools[index]?.name === name && reply.tools[index]?.ok === null))
        index--;
      if (index === -1) return reply;
      const ok = field(event.data, 'ok') === true;
      return { ...reply, tools: reply.tools.map((t, i) => (i === index ? { ...t, ok } : t)) };
    }
    case 'error':
      return { ...reply, error: text(event.data, 'message') };
    default:
      return reply;
  }
}

/**
 * One user's assistant conversation: sends a message, streams the reply into the last entry and
 * keeps the whole conversation in browser storage under the user's auth subject (design §3.3, §4).
 */
export function useAssistantChat(subject: string): AssistantChat {
  const { t } = useTranslation();
  const [conversation, setConversation] = useState<Conversation>(() => loadConversation(subject));
  const [streaming, setStreaming] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(
    () => () => {
      abortRef.current?.abort();
    },
    [],
  );

  async function send(message: string): Promise<void> {
    const trimmed = message.trim();
    if (!trimmed || streaming) return;

    const controller = new AbortController();
    abortRef.current = controller;
    setStreaming(true);

    const base = conversation;
    const user: AssistantEntry = { role: 'user', text: trimmed };
    let reply: AssistantReply = { role: 'assistant', text: '', tools: [] };
    let history = base.history;
    const show = () => {
      setConversation({ history, entries: [...base.entries, user, reply] });
    };
    show();

    try {
      const response = await assistantFetch('/messages', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          message: trimmed,
          timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
          messages: base.history,
        }),
        signal: controller.signal,
      });

      if (!response.ok || !response.body) {
        reply = {
          ...reply,
          error: t(response.status === 429 ? 'assistant.limit_reached' : 'assistant.failed'),
        };
      } else {
        for await (const event of readEvents(response.body)) {
          if (event.type === 'history') {
            const added = field(event.data, 'messages');
            if (Array.isArray(added)) history = [...history, ...(added as unknown[])];
          } else {
            reply = applyEvent(reply, event);
          }
          show();
        }
      }
    } catch {
      if (controller.signal.aborted) return;
      reply = { ...reply, error: t('assistant.failed') };
    } finally {
      if (abortRef.current === controller) {
        abortRef.current = null;
        setStreaming(false);
      }
    }

    show();
    saveConversation(subject, { history, entries: [...base.entries, user, reply] });
  }

  function clear(): void {
    abortRef.current?.abort();
    abortRef.current = null;
    setStreaming(false);
    setConversation(EMPTY_CONVERSATION);
    clearConversation(subject);
  }

  return { entries: conversation.entries, streaming, send, clear };
}
