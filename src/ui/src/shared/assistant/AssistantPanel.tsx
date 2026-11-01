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
  FileText,
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
import { ACCEPTED_DOCUMENT_TYPES, readDocument } from './readDocument';
import { useAssistantChat } from './useAssistantChat';

import type { AssistantEntry, PendingAction, ToolActivity } from './conversation';
import type { AttachedImage } from './downscaleImage';
import type { AttachedDocument } from './readDocument';

export const OPEN_ASSISTANT_EVENT = 'home-system:open-assistant';

export interface AssistantPanelProps {
  /** Auth subject of the signed-in user; the conversation is stored under it. */
  subject: string;
}

/**
 * The assistant as a floating, non-modal card (full-screen sheet on phones), opened through
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
  const [document, setDocument] = useState<AttachedDocument | null>(null);
  const fileInput = useRef<HTMLInputElement>(null);
  const documentInput = useRef<HTMLInputElement>(null);

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

  async function handleDocument(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (file) setDocument(await readDocument(file));
  }

  function handleKeyDown(event: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
      event.preventDefault();
      event.currentTarget.form?.requestSubmit();
    }
  }

  return (
    // Non-modal: no overlay, the page stays usable; only Escape or the close button dismiss it.
    <Sheet open={open} onOpenChange={setOpen} modal={false}>
      <SheetContent
        side="right"
        onInteractOutside={(event) => {
          event.preventDefault();
        }}
        className="bg-card flex h-full flex-col gap-0 p-0 sm:inset-auto sm:right-4 sm:bottom-4 sm:h-160 sm:max-h-[85dvh] sm:w-96 sm:rounded-xl sm:border sm:shadow-xl"
      >
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
            void chat.send(draft, {
              ...(image ? { image } : {}),
              ...(document ? { document } : {}),
            });
            dropImage();
            setDocument(null);
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
          {document && (
            <div className="flex items-center gap-2 text-xs">
              <FileText className="text-muted-foreground size-4 shrink-0" />
              <span className="truncate">{document.name}</span>
              <Button
                type="button"
                variant="ghost"
                size="xs"
                onClick={() => {
                  setDocument(null);
                }}
                aria-label={t('assistant.remove_document')}
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
            <input
              ref={documentInput}
              type="file"
              accept={ACCEPTED_DOCUMENT_TYPES.join(',')}
              className="hidden"
              aria-label={t('assistant.attach_document')}
              onChange={(event) => {
                void handleDocument(event);
              }}
            />
            <Button
              type="button"
              variant="ghost"
              size="xs"
              className="ms-auto"
              disabled={chat.waiting}
              onClick={() => documentInput.current?.click()}
              title={t('assistant.attach_document')}
            >
              <FileText className="size-4" />
              <span className="sr-only">{t('assistant.attach_document')}</span>
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
                  disabled={chat.waiting}
                  onClick={() => fileInput.current?.click()}
                  title={t('assistant.attach_image')}
                >
                  <Paperclip className="size-4" />
                  <span className="sr-only">{t('assistant.attach_image')}</span>
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
        {entry.document && (
          <span className="text-muted-foreground flex items-center gap-1 text-xs">
            <FileText className="size-3.5" />
            {t('assistant.document_attached', { name: entry.document })}
          </span>
        )}
      </div>
    );
  }

  return (
    <div className="flex max-w-[85%] min-w-0 flex-col gap-1.5 self-start">
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
      <dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-3 gap-y-1 text-xs">
        {Object.entries(action.arguments)
          // Server-minted keys for retries and plan previews; they mean nothing to the user.
          .filter(([key]) => key !== 'operationKey' && key !== 'previewToken')
          .map(([key, value]) => (
            <div key={key} className="contents">
              <dt className="text-muted-foreground">{key}</dt>
              {isRows(value) ? (
                <dd className="col-span-2 overflow-x-auto">
                  <RowsTable rows={value} />
                </dd>
              ) : (
                <dd className="wrap-anywhere">
                  {typeof value === 'string' ? value : JSON.stringify(value)}
                </dd>
              )}
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

type Row = Record<string, unknown>;

function isRows(value: unknown): value is Row[] {
  return (
    Array.isArray(value) &&
    value.length > 0 &&
    value.every((item) => typeof item === 'object' && item !== null && !Array.isArray(item))
  );
}

// Matching keys and person ids mean nothing to the user; the card shows what gets recorded.
const HIDDEN_COLUMN = /(Key|Id|Ids)$/;

/** An argument that is a list of records (an import's rows) as a compact table. */
function RowsTable({ rows }: { rows: Row[] }) {
  const columns = [...new Set(rows.flatMap((row) => Object.keys(row)))].filter(
    (key) => !HIDDEN_COLUMN.test(key),
  );

  return (
    <table className="w-full text-start">
      <thead>
        <tr>
          {columns.map((column) => (
            <th key={column} scope="col" className="text-muted-foreground pe-2 font-normal">
              {column}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map((row, i) => (
          <tr key={i}>
            {columns.map((column) => {
              const cell = row[column];
              return (
                <td key={column} className="pe-2 align-top">
                  {cell === null || cell === undefined
                    ? ''
                    : typeof cell === 'string'
                      ? cell
                      : JSON.stringify(cell)}
                </td>
              );
            })}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
