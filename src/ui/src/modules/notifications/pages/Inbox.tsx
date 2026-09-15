import { Banner, Button, Checkbox, EmptyState } from '@shared/components/ui';
import { Inbox as InboxIcon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { useBulkMarkRead } from '../api/hooks/useBulkMarkRead';
import { useMarkRead } from '../api/hooks/useMarkRead';
import { useNotifications } from '../api/hooks/useNotifications';
import { NotificationListItem } from '../components/NotificationListItem';

const PAGE_SIZE = 20;

export default function Inbox() {
  const { t } = useTranslation('notifications');
  const [page, setPage] = useState(1);
  const { data, isLoading, isError, refetch } = useNotifications({ page, pageSize: PAGE_SIZE });
  const markRead = useMarkRead();
  const bulkMarkRead = useBulkMarkRead();

  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const id = window.setInterval(() => {
      setNow(new Date());
    }, 60_000);
    return () => {
      window.clearInterval(id);
    };
  }, []);

  const items = data?.items ?? [];
  const totalPages = Number(data?.totalPages ?? 0);

  const unreadIdsOnPage = items.filter((n) => !n.readAt).map((n) => n.id);

  const [rawSelected, setSelected] = useState<Set<string>>(() => new Set());

  // Always mask selection by what's currently visible — drops ids removed by
  // pagination, refetch, or another tab marking the row read. Pure derived
  // state, no extra effect needed.
  const visibleUnread = new Set(unreadIdsOnPage);
  const selected = new Set<string>();
  rawSelected.forEach((id) => {
    if (visibleUnread.has(id)) selected.add(id);
  });

  const allSelected = unreadIdsOnPage.length > 0 && selected.size === unreadIdsOnPage.length;
  const partiallySelected = selected.size > 0 && !allSelected;

  function toggleOne(id: string, next: boolean) {
    setSelected((prev) => {
      const out = new Set(prev);
      if (next) out.add(id);
      else out.delete(id);
      return out;
    });
  }

  function toggleAll(next: boolean) {
    setSelected(next ? new Set(unreadIdsOnPage) : new Set());
  }

  function clearSelection() {
    setSelected(new Set());
  }

  async function applyBulkRead() {
    if (selected.size === 0) return;
    const ids = Array.from(selected);
    try {
      await bulkMarkRead.mutateAsync(ids);
      clearSelection();
    } catch {
      // mutation hook reports error via state; nothing more to do here
    }
  }

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-6 md:px-8">
      <header className="mb-6 flex items-end justify-between gap-4">
        <div>
          <h1 className="text-[26px] font-bold tracking-tight">{t('inbox.title')}</h1>
          <p className="text-muted-foreground mt-0.5 text-sm">{t('inbox.subtitle')}</p>
        </div>
        <Button asChild variant="secondary" size="sm">
          <Link to="/notifications/preferences">{t('inbox.preferences_link')}</Link>
        </Button>
      </header>

      {isLoading && (
        <ul aria-busy="true" className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <li key={i} className="bg-muted h-20 animate-pulse rounded-[13px]" />
          ))}
        </ul>
      )}

      {isError && (
        <Banner variant="error" onRetry={() => void refetch()} retryLabel={t('inbox.retry')}>
          {t('inbox.error')}
        </Banner>
      )}

      {!isLoading && !isError && items.length === 0 && (
        <div className="bg-card rounded-[22px] border">
          <EmptyState
            icon={InboxIcon}
            title={t('inbox.empty_title')}
            description={t('inbox.empty_body')}
          />
        </div>
      )}

      {!isLoading && !isError && items.length > 0 && (
        <>
          {unreadIdsOnPage.length > 0 ? (
            <div className="bg-accent mb-3 flex items-center gap-3 rounded-[13px] px-3 py-2">
              <Checkbox
                checked={allSelected}
                ref={(el) => {
                  if (el) el.indeterminate = partiallySelected;
                }}
                aria-label={t('inbox.select_all_aria')}
                onChange={(e) => {
                  toggleAll(e.target.checked);
                }}
              />
              {selected.size > 0 ? (
                <>
                  <span className="text-sm font-medium">
                    {t('inbox.selected_count', { count: selected.size })}
                  </span>
                  <div className="ml-auto flex items-center gap-2">
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={clearSelection}
                      disabled={bulkMarkRead.isPending}
                    >
                      {t('inbox.cancel_selection')}
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      onClick={() => void applyBulkRead()}
                      disabled={bulkMarkRead.isPending}
                    >
                      {t('inbox.mark_selected_read', { count: selected.size })}
                    </Button>
                  </div>
                </>
              ) : (
                <span className="text-muted-foreground text-sm">{t('inbox.select_hint')}</span>
              )}
            </div>
          ) : null}

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
                  selected={selected.has(n.id)}
                  onToggleSelect={toggleOne}
                />
              ) : null,
            )}
          </ul>

          {totalPages > 1 && (
            <nav
              aria-label="Pagination"
              className="mt-6 flex items-center justify-between gap-2 text-sm"
            >
              <Button
                type="button"
                variant="secondary"
                size="sm"
                disabled={page <= 1}
                onClick={() => {
                  setPage((p) => Math.max(1, p - 1));
                }}
              >
                {t('inbox.previous_page')}
              </Button>
              <span className="text-muted-foreground tnum">
                {t('inbox.page_indicator', { page, totalPages })}
              </span>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => {
                  setPage((p) => p + 1);
                }}
              >
                {t('inbox.next_page')}
              </Button>
            </nav>
          )}
        </>
      )}
    </main>
  );
}
