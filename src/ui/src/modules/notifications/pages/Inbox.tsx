import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { useMarkRead } from '../api/hooks/useMarkRead';
import { useNotifications } from '../api/hooks/useNotifications';
import { NotificationListItem } from '../components/NotificationListItem';

const PAGE_SIZE = 20;

export default function Inbox() {
  const { t } = useTranslation('notifications');
  const [page, setPage] = useState(1);
  const { data, isLoading, isError, refetch } = useNotifications({ page, pageSize: PAGE_SIZE });
  const markRead = useMarkRead();

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
  const totalPages = data?.totalPages ?? 0;

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
