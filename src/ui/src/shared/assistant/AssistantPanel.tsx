import { userManager } from '@shared/auth/userManager';
import {
  Button,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  Textarea,
} from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import { Check, Loader2, SendHorizontal, Trash2, Wrench, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useAssistantStatus } from './assistantStatus';
import { clearAllConversations } from './conversation';
import { useAssistantChat } from './useAssistantChat';

import type { AssistantEntry, ToolActivity } from './conversation';

export const OPEN_ASSISTANT_EVENT = 'home-system:open-assistant';

export interface AssistantPanelProps {
  /** Auth subject of the signed-in user; the conversation is stored under it. */
  subject: string;
}

/**
 * The assistant side panel (full-screen sheet on phones), opened through
 * {@link OPEN_ASSISTANT_EVENT}. Stays mounted while closed so a reply keeps streaming.
 * Renders nothing while the assistant is switched off. Design §9.
 */
export function AssistantPanel({ subject }: AssistantPanelProps) {
  const { t } = useTranslation();
  const { data: status } = useAssistantStatus();
  const chat = useAssistantChat(subject);
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState('');

  useEffect(() => {
    const onOpen = () => {
      setOpen(true);
    };
    window.addEventListener(OPEN_ASSISTANT_EVENT, onOpen);
    return () => {
      window.removeEventListener(OPEN_ASSISTANT_EVENT, onOpen);
    };
  }, []);

  // Every sign-out path (sidebar, more sheet, preferences) removes the OIDC user first.
  useEffect(() => {
    userManager.events.addUserUnloaded(clearAllConversations);
    return () => {
      userManager.events.removeUserUnloaded(clearAllConversations);
    };
  }, []);

  if (!status) return null;

  function handleKeyDown(event: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
      event.preventDefault();
      event.currentTarget.form?.requestSubmit();
    }
  }

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetContent side="right" className="bg-card flex h-full flex-col gap-0 p-0">
        <SheetHeader className="border-border border-b pe-12 pb-4">
          <SheetTitle>{t('assistant.title')}</SheetTitle>
          <SheetDescription>
            {t('assistant.provider_notice', { provider: status.provider })}
          </SheetDescription>
        </SheetHeader>

        <div
          role="log"
          aria-live="polite"
          aria-label={t('assistant.conversation')}
          className="flex flex-1 flex-col gap-3 overflow-y-auto p-4"
        >
          {chat.entries.length === 0 ? (
            <p className="text-muted-foreground m-auto max-w-xs text-center text-sm">
              {t('assistant.empty')}
            </p>
          ) : (
            chat.entries.map((entry, i) => <Entry key={i} entry={entry} />)
          )}
        </div>

        <form
          onSubmit={(event) => {
            event.preventDefault();
            setDraft('');
            void chat.send(draft);
          }}
          className="border-border flex flex-col gap-2 border-t p-4"
        >
          <Textarea
            value={draft}
            onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => {
              setDraft(e.target.value);
            }}
            onKeyDown={handleKeyDown}
            aria-label={t('assistant.message_label')}
            placeholder={t('assistant.placeholder')}
            maxLength={8000}
            rows={2}
            className="min-h-0 resize-none"
          />
          <div className="flex items-center justify-between gap-2">
            <Button
              type="button"
              variant="ghost"
              size="xs"
              onClick={chat.clear}
              disabled={chat.entries.length === 0}
            >
              <Trash2 className="size-4" />
              {t('assistant.clear')}
            </Button>
            <Button type="submit" size="xs" disabled={chat.streaming || !draft.trim()}>
              {chat.streaming ? (
                <Loader2 className="size-4 animate-spin motion-reduce:animate-none" />
              ) : (
                <SendHorizontal className="size-4" />
              )}
              {t('assistant.send')}
            </Button>
          </div>
        </form>
      </SheetContent>
    </Sheet>
  );
}

function Entry({ entry }: { entry: AssistantEntry }) {
  if (entry.role === 'user') {
    return (
      <p className="bg-primary text-primary-foreground max-w-[85%] self-end rounded-lg px-3 py-2 text-sm whitespace-pre-wrap">
        {entry.text}
      </p>
    );
  }

  return (
    <div className="flex max-w-[85%] flex-col gap-1.5 self-start">
      {entry.tools.length > 0 && (
        <ul className="flex flex-col gap-1">
          {entry.tools.map((tool, i) => (
            <ToolLine key={i} tool={tool} />
          ))}
        </ul>
      )}
      {entry.text && (
        <p className="bg-muted rounded-lg px-3 py-2 text-sm whitespace-pre-wrap">{entry.text}</p>
      )}
      {entry.error && (
        <p role="alert" className="text-destructive text-sm">
          {entry.error}
        </p>
      )}
    </div>
  );
}

function ToolLine({ tool }: { tool: ToolActivity }) {
  const { t } = useTranslation();
  const state = tool.ok === null ? 'running' : tool.ok ? 'ok' : 'failed';
  const Icon = { running: Loader2, ok: Check, failed: X }[state];

  return (
    <li className="text-muted-foreground flex items-center gap-1.5 text-xs">
      <Wrench className="size-3.5 shrink-0" />
      <span className="truncate">
        <span className="text-foreground font-medium">{tool.name}</span>
        {tool.summary ? ` · ${tool.summary}` : ''}
      </span>
      <Icon
        aria-label={t(`assistant.tool_${state}`)}
        className={cn(
          'size-3.5 shrink-0',
          state === 'running' && 'animate-spin motion-reduce:animate-none',
          state === 'failed' && 'text-destructive',
        )}
      />
    </li>
  );
}
