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
import {
  Check,
  ImageIcon,
  Loader2,
  Paperclip,
  SendHorizontal,
  ShieldAlert,
  Trash2,
  Wrench,
  X,
} from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useAssistantStatus } from './assistantStatus';
import { clearAllConversations } from './conversation';
import { ACCEPTED_IMAGE_TYPES, downscaleImage } from './downscaleImage';
import { useAssistantChat } from './useAssistantChat';

import type { AssistantEntry, PendingAction, ToolActivity } from './conversation';
import type { AttachedImage } from './downscaleImage';

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
  const [image, setImage] = useState<AttachedImage | null>(null);
  const [imageError, setImageError] = useState<string | null>(null);
  const fileInput = useRef<HTMLInputElement>(null);

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

  function dropImage() {
    if (image) URL.revokeObjectURL(image.previewUrl);
    setImage(null);
  }

  async function handleFile(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;
    const result = await downscaleImage(file);
    dropImage();
    if (result.ok) {
      setImage(result.image);
      setImageError(null);
    } else {
      setImageError(t(`assistant.image_${result.reason}`));
    }
  }

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
            chat.entries.map((entry, i) => (
              <Entry
                key={i}
                entry={entry}
                busy={chat.streaming}
                onDecide={(confirm) => {
                  void chat.decide(i, confirm);
                }}
              />
            ))
          )}
        </div>

        <form
          onSubmit={(event) => {
            event.preventDefault();
            setDraft('');
            void chat.send(draft, image ?? undefined);
            dropImage();
            setImageError(null);
          }}
          className="border-border flex flex-col gap-2 border-t p-4"
        >
          {image && (
            <div className="flex items-center gap-2">
              <img
                src={image.previewUrl}
                alt={t('assistant.image_preview')}
                className="border-border size-14 rounded-md border object-cover"
              />
              <Button
                type="button"
                variant="ghost"
                size="xs"
                onClick={dropImage}
                aria-label={t('assistant.remove_image')}
              >
                <X className="size-4" />
              </Button>
            </div>
          )}
          {imageError && (
            <p role="alert" className="text-destructive text-xs">
              {imageError}
            </p>
          )}
          <Textarea
            value={draft}
            onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => {
              setDraft(e.target.value);
            }}
            onKeyDown={handleKeyDown}
            aria-label={t('assistant.message_label')}
            placeholder={t(chat.waiting ? 'assistant.decide_first' : 'assistant.placeholder')}
            disabled={chat.waiting}
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
            {status.images && (
              <>
                <input
                  ref={fileInput}
                  type="file"
                  accept={ACCEPTED_IMAGE_TYPES.join(',')}
                  className="hidden"
                  aria-label={t('assistant.attach_image')}
                  onChange={(event) => {
                    void handleFile(event);
                  }}
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="xs"
                  className="ms-auto"
                  disabled={chat.waiting}
                  onClick={() => fileInput.current?.click()}
                >
                  <Paperclip className="size-4" />
                  {t('assistant.attach_image')}
                </Button>
              </>
            )}
            <Button
              type="submit"
              size="xs"
              disabled={chat.streaming || chat.waiting || !draft.trim()}
            >
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

interface EntryProps {
  entry: AssistantEntry;
  busy: boolean;
  onDecide: (confirm: boolean) => void;
}

function Entry({ entry, busy, onDecide }: EntryProps) {
  const { t } = useTranslation();

  if (entry.role === 'user') {
    return (
      <div className="flex max-w-[85%] flex-col items-end gap-1 self-end">
        <p className="bg-primary text-primary-foreground rounded-lg px-3 py-2 text-sm whitespace-pre-wrap">
          {entry.text}
        </p>
        {entry.image && (
          <span className="text-muted-foreground flex items-center gap-1 text-xs">
            <ImageIcon className="size-3.5" />
            {t('assistant.image_attached')}
          </span>
        )}
      </div>
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
      {entry.pending && <PendingCard action={entry.pending} busy={busy} onDecide={onDecide} />}
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

interface PendingCardProps {
  action: PendingAction;
  busy: boolean;
  onDecide: (confirm: boolean) => void;
}

/** The preview of a held write: exactly the arguments Confirm will run (design §6). */
function PendingCard({ action, busy, onDecide }: PendingCardProps) {
  const { t } = useTranslation();
  const titleId = `pending-${action.token.slice(-12)}`;

  return (
    <section
      aria-labelledby={titleId}
      className={cn(
        'bg-card flex flex-col gap-2 rounded-lg border p-3 text-sm',
        action.destructive ? 'border-destructive' : 'border-border',
      )}
    >
      <h3 id={titleId} className="flex items-center gap-1.5 font-medium">
        {action.destructive && <ShieldAlert className="text-destructive size-4 shrink-0" />}
        {t(action.destructive ? 'assistant.pending_destructive' : 'assistant.pending_title')}
      </h3>
      <p className="text-muted-foreground text-xs">{action.name}</p>
      <dl className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-xs">
        {Object.entries(action.arguments)
          // The server mints the key for retries; it means nothing to the user.
          .filter(([key]) => key !== 'operationKey')
          .map(([key, value]) => (
            <div key={key} className="contents">
              <dt className="text-muted-foreground">{key}</dt>
              <dd className="break-words">
                {typeof value === 'string' ? value : JSON.stringify(value)}
              </dd>
            </div>
          ))}
      </dl>
      {action.status === 'waiting' ? (
        <div className="flex justify-end gap-2">
          <Button
            type="button"
            variant="ghost"
            size="xs"
            disabled={busy}
            onClick={() => {
              onDecide(false);
            }}
          >
            {t('assistant.cancel_action')}
          </Button>
          <Button
            type="button"
            variant={action.destructive ? 'destructive' : 'default'}
            size="xs"
            disabled={busy}
            onClick={() => {
              onDecide(true);
            }}
          >
            {t('assistant.confirm_action')}
          </Button>
        </div>
      ) : (
        <p className="text-muted-foreground text-xs">
          {t(
            action.status === 'confirmed'
              ? 'assistant.action_confirmed'
              : 'assistant.action_cancelled',
          )}
        </p>
      )}
    </section>
  );
}
