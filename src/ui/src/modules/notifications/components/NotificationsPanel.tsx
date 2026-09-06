import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@shared/components/ui';
import { Bell } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { useMarkRead } from '../api/hooks/useMarkRead';
import { useNotifications } from '../api/hooks/useNotifications';

import { NotificationListItem } from './NotificationListItem';
import { UnreadBadge } from './UnreadBadge';

const PANEL_PAGE_SIZE = 10;

export function NotificationsPanel() {
  const { t } = useTranslation('notifications');
  const [open, setOpen] = useState(false);
  const { data, isLoading, isError, refetch } = useNotifications({
    page: 1,
    pageSize: PANEL_PAGE_SIZE,
  });
  const markRead = useMarkRead();

  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    if (!open) return;
    const id = window.setInterval(() => {
      setNow(new Date());
    }, 60_000);
    return () => {
      window.clearInterval(id);
    };
  }, [open]);

  const items = useMemo(() => data?.items ?? [], [data]);

  function close() {
    setOpen(false);
  }

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger
        aria-label={t('panel.open_aria')}
        className="text-muted-foreground hover:text-foreground hover:bg-accent/60 focus-visible:ring-primary relative inline-flex h-9 w-9 items-center justify-center rounded-md transition-colors focus-visible:ring-2 focus-visible:outline-none"
      >
        <Bell className="h-5 w-5" />
        <span className="pointer-events-none absolute -top-1 -right-1">
          <UnreadBadge />
        </span>
      </SheetTrigger>
      <SheetContent side="right" className="bg-card flex h-full flex-col gap-0 p-0" onClose={close}>
        <SheetHeader className="border-border border-b">
          <SheetTitle>{t('panel.title')}</SheetTitle>
          <SheetDescription>{t('panel.subtitle')}</SheetDescription>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto p-4">
          {isLoading && (
            <ul aria-busy="true" className="space-y-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <li key={i} className="bg-muted h-20 animate-pulse rounded-md" />
              ))}
            </ul>
          )}

          {isError && (
            <div
              role="alert"
              className="border-destructive/40 bg-destructive/10 text-foreground flex items-center justify-between rounded-md border p-4 text-sm"
            >
              <p>{t('inbox.error')}</p>
              <button
                type="button"
                onClick={() => void refetch()}
                className="border-destructive/40 focus-visible:ring-primary hover:bg-destructive/20 rounded-md border px-3 py-1 text-sm focus-visible:ring-2 focus-visible:outline-none"
              >
                {t('inbox.retry')}
              </button>
            </div>
          )}

          {!isLoading && !isError && items.length === 0 && (
            <div className="border-border rounded-md border border-dashed p-8 text-center">
              <p className="text-foreground text-base font-medium">{t('inbox.empty_title')}</p>
              <p className="text-muted-foreground mt-1 text-sm">{t('inbox.empty_body')}</p>
            </div>
          )}

          {!isLoading && !isError && items.length > 0 && (
            <ul className="space-y-2">
              {items.map((n) =>
                n.id ? (
                  <NotificationListItem
                    key={n.id}
                    notification={n}
                    onActivate={(id) => {
                      markRead.mutate(id);
                    }}
                    now={now}
                  />
                ) : null,
              )}
            </ul>
          )}
        </div>

        <footer className="border-border flex items-center justify-between gap-2 border-t p-4 text-sm">
          <Link
            to="/notifications"
            onClick={close}
            className="text-primary focus-visible:ring-primary hover:underline focus-visible:ring-2 focus-visible:outline-none"
          >
            {t('panel.view_all')}
          </Link>
          <Link
            to="/notifications/preferences"
            onClick={close}
            className="text-primary focus-visible:ring-primary hover:underline focus-visible:ring-2 focus-visible:outline-none"
          >
            {t('panel.preferences_link')} →
          </Link>
        </footer>
      </SheetContent>
    </Sheet>
  );
}
