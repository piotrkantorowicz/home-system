import { Button, Checkbox } from '@shared/components/ui';
import { useEffect, useMemo, useState } from 'react';
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

  const items = useMemo(() => data?.items ?? [], [data]);
  const totalPages = Number(data?.totalPages ?? 0);

  const unreadIdsOnPage = useMemo(() => items.filter((n) => !n.readAt).map((n) => n.id), [items]);

  const [rawSelected, setSelected] = useState<Set<string>>(() => new Set());

  // Always mask selection by what's currently visible — drops ids removed by
  // pagination, refetch, or another tab marking the row read. Pure derived
  // state, no extra effect needed.
  const selected = useMemo(() => {
    const visible = new Set(unreadIdsOnPage);
    const out = new Set<string>();
    rawSelected.forEach((id) => {
      if (visible.has(id)) out.add(id);
    });
    return out;
  }, [rawSelected, unreadIdsOnPage]);

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
    <main className="mx-auto w-full max-w-3xl px-4 py-6">
      <header className="mb-6 flex items-baseline justify-between">
        <div>
          <h1 className="text-text text-2xl font-semibold">{t('inbox.title')}</h1>
          <p className="text-text-muted text-sm">{t('inbox.subtitle')}</p>
        </div>
        <Link
          to="/notifications/preferences"
          className="text-primary focus-visible:ring-primary text-sm hover:underline focus-visible:ring-2 focus-visible:outline-none"
        >
          {t('inbox.preferences_link')} →
        </Link>
      </header>

      {isLoading && (
        <ul aria-busy="true" className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <li key={i} className="bg-surface-alt h-20 animate-pulse rounded-md" />
          ))}
        </ul>
      )}

      {isError && (
        <div
          role="alert"
          className="border-error/40 bg-error/10 text-text flex items-center justify-between rounded-md border p-4 text-sm"
        >
          <p>{t('inbox.error')}</p>
          <button
            type="button"
            onClick={() => void refetch()}
            className="border-error/40 focus-visible:ring-primary hover:bg-error/20 rounded-md border px-3 py-1 text-sm focus-visible:ring-2 focus-visible:outline-none"
          >
            {t('inbox.retry')}
          </button>
        </div>
      )}

      {!isLoading && !isError && items.length === 0 && (
        <div className="border-border rounded-md border border-dashed p-8 text-center">
          <p className="text-text text-base font-medium">{t('inbox.empty_title')}</p>
          <p className="text-text-muted mt-1 text-sm">{t('inbox.empty_body')}</p>
        </div>
      )}

      {!isLoading && !isError && items.length > 0 && (
        <>
          {unreadIdsOnPage.length > 0 ? (
            <div className="bg-surface-alt mb-3 flex items-center gap-3 rounded-md px-3 py-2">
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
                  <span className="text-text text-sm font-medium">
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
                <span className="text-text-muted text-sm">{t('inbox.select_hint')}</span>
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
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => {
                  setPage((p) => Math.max(1, p - 1));
                }}
                className="border-border bg-surface focus-visible:ring-primary rounded-md border px-3 py-1 focus-visible:ring-2 focus-visible:outline-none disabled:opacity-50"
              >
                {t('inbox.previous_page')}
              </button>
              <span className="text-text-muted">
                {t('inbox.page_indicator', { page, totalPages })}
              </span>
              <button
                type="button"
                disabled={page >= totalPages}
                onClick={() => {
                  setPage((p) => p + 1);
                }}
                className="border-border bg-surface focus-visible:ring-primary rounded-md border px-3 py-1 focus-visible:ring-2 focus-visible:outline-none disabled:opacity-50"
              >
                {t('inbox.next_page')}
              </button>
            </nav>
          )}
        </>
      )}
    </main>
  );
}
