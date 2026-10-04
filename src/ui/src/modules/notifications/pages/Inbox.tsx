import {
  Banner,
  Button,
  EmptyState,
  PageContainer,
  PageHeader,
  SegmentedControl,
} from '@shared/components/ui';
import { CheckCheck, Inbox as InboxIcon, Settings } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { useMarkAllRead } from '../api/hooks/useMarkAllRead';
import { useMarkRead } from '../api/hooks/useMarkRead';
import { useNotifications } from '../api/hooks/useNotifications';
import { useUnreadCount } from '../api/hooks/useUnreadCount';
import { useUnreadNotifications } from '../api/hooks/useUnreadNotifications';
import { NotificationListItem } from '../components/NotificationListItem';

import type { NotificationDto } from '../api/hooks/useNotifications';

const PAGE_SIZE = 20;

type Filter = 'all' | 'unread';

function isSameDay(a: Date, b: Date): boolean {
  return a.toDateString() === b.toDateString();
}

export default function Inbox() {
  const { t } = useTranslation('notifications');
  const [filter, setFilter] = useState<Filter>('all');
  const [page, setPage] = useState(1);
  const markRead = useMarkRead();
  const markAll = useMarkAllRead();

  // "All" pages on the server; "Unread" has no server filter, so it loads every unread row and pages locally.
  const all = useNotifications({ page, pageSize: PAGE_SIZE, enabled: filter === 'all' });
  const unread = useUnreadNotifications(filter === 'unread');
  const { data: unreadCount } = useUnreadCount();
  const unreadTotal = unreadCount ? Number(unreadCount.total) : 0;

  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const id = window.setInterval(() => {
      setNow(new Date());
    }, 60_000);
    return () => {
      window.clearInterval(id);
    };
  }, []);

  const active = filter === 'all' ? all : unread;
  // Filtering on readAt lets a row leave the Unread view the moment it is marked read (optimistic update).
  const unreadItems = (unread.data?.items ?? []).filter((n) => !n.readAt);
  const items: NotificationDto[] =
    filter === 'all'
      ? (all.data?.items ?? [])
      : unreadItems.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const totalPages =
    filter === 'all'
      ? Number(all.data?.totalPages ?? 0)
      : Math.ceil(unreadItems.length / PAGE_SIZE);

  function changeFilter(next: Filter) {
    setFilter(next);
    setPage(1);
  }

  const groups = [
    {
      id: 'today',
      label: t('inbox.group_today'),
      rows: items.filter((n) => isSameDay(new Date(n.createdAt), now)),
    },
    {
      id: 'earlier',
      label: t('inbox.group_earlier'),
      rows: items.filter((n) => !isSameDay(new Date(n.createdAt), now)),
    },
  ].filter((g) => g.rows.length > 0);

  const { isLoading, isError, refetch } = active;

  return (
    <PageContainer width="narrow">
      <PageHeader
        title={t('inbox.title')}
        subtitle={
          unreadTotal > 0
            ? t('inbox.subtitle_unread', { count: unreadTotal })
            : t('inbox.caught_up')
        }
        actions={
          <>
            <Button
              type="button"
              variant="secondary"
              size="sm"
              disabled={unreadTotal === 0 || markAll.isPending}
              onClick={() => {
                markAll.mutate();
              }}
            >
              <CheckCheck className="size-4" aria-hidden />
              {t('inbox.mark_all_read')}
            </Button>
            <Button asChild variant="secondary" size="sm">
              <Link to="/settings/app" aria-label={t('inbox.settings_aria')}>
                <Settings className="size-4" aria-hidden />
              </Link>
            </Button>
          </>
        }
      />

      <div className="mb-4">
        <SegmentedControl<Filter>
          label={t('inbox.filter_label')}
          value={filter}
          onChange={changeFilter}
          options={[
            { value: 'all', label: t('inbox.filter_all') },
            { value: 'unread', label: t('inbox.filter_unread', { count: unreadTotal }) },
          ]}
        />
      </div>

      {markAll.isError ? (
        <div className="mb-4">
          <Banner variant="error">{t('inbox.mark_all_error')}</Banner>
        </div>
      ) : null}

      {isLoading && (
        <ul aria-busy="true" className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <li key={i} className="bg-muted rounded-13px h-20 animate-pulse" />
          ))}
        </ul>
      )}

      {isError && (
        <Banner variant="error" onRetry={() => void refetch()} retryLabel={t('inbox.retry')}>
          {t('inbox.error')}
        </Banner>
      )}

      {!isLoading && !isError && items.length === 0 && (
        <div className="bg-card rounded-22px border">
          {filter === 'unread' ? (
            <EmptyState icon={InboxIcon} title={t('inbox.caught_up')} />
          ) : (
            <EmptyState
              icon={InboxIcon}
              title={t('inbox.empty_title')}
              description={t('inbox.empty_body')}
            />
          )}
        </div>
      )}

      {!isLoading &&
        !isError &&
        groups.map((g) => (
          <section key={g.id} aria-labelledby={`inbox-${g.id}`} className="mb-6">
            <h2
              id={`inbox-${g.id}`}
              className="text-muted-foreground text-11px mb-2 font-semibold tracking-wider uppercase"
            >
              {g.label}
            </h2>
            <ul className="space-y-2">
              {g.rows.map((n) => (
                <NotificationListItem
                  key={n.id}
                  notification={n}
                  onActivate={(id) => {
                    markRead.mutate(id);
                  }}
                  now={now}
                />
              ))}
            </ul>
          </section>
        ))}

      {!isLoading && !isError && totalPages > 1 && (
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
    </PageContainer>
  );
}
