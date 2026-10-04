import { useNavigationAccess } from '@shared/context/NavigationAccessContext';
import { cn } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { formatTimeAgo } from '../utils/timeAgo';
import { getTypeMeta } from '../utils/typeMeta';

import type { NotificationDto } from '../api/hooks/useNotifications';

export interface NotificationListItemProps {
  notification: NotificationDto;
  /** Called when an unread item is opened, so the caller can mark it read. */
  onActivate: (id: string) => void;
  now: Date;
  /** Called after navigating to the item's destination (e.g. to close the panel it sits in). */
  onNavigate?: () => void;
}

/** One notification: a single button that marks it read and opens its destination. */
export function NotificationListItem({
  notification,
  onActivate,
  now,
  onNavigate,
}: NotificationListItemProps) {
  const { t, i18n } = useTranslation('notifications');
  const navigate = useNavigate();
  const access = useNavigationAccess();
  const meta = getTypeMeta(notification.type);
  const Icon = meta.icon;
  const isUnread = !notification.readAt;
  // Only offer destinations the user may reach (the API still enforces access).
  const destination =
    meta.destination && access.canNavigate(meta.destination.href) ? meta.destination : undefined;

  function handleActivate() {
    if (isUnread) onActivate(notification.id);
    if (destination) {
      void navigate(destination.href);
      onNavigate?.();
    }
  }

  return (
    <li>
      <button
        type="button"
        onClick={handleActivate}
        className={cn(
          'border-border bg-card rounded-16px flex w-full items-start gap-3 border p-3.5 text-left transition-colors',
          'hover:bg-accent focus-visible:ring-primary focus-visible:ring-2 focus-visible:outline-none',
        )}
      >
        <span className="bg-accent text-primary rounded-13px flex size-10 shrink-0 items-center justify-center">
          <Icon aria-hidden className="size-5" />
        </span>
        <span className="min-w-0 flex-1">
          <span className="flex items-baseline justify-between gap-2">
            <span
              className={cn(
                'text-foreground truncate text-sm',
                isUnread ? 'font-bold' : 'font-medium',
              )}
            >
              {notification.title}
            </span>
            <span className="text-muted-foreground shrink-0 text-xs">
              {notification.createdAt
                ? formatTimeAgo(notification.createdAt, i18n.language, now)
                : ''}
            </span>
          </span>
          <span className="text-text-2 mt-1 line-clamp-2 block text-sm">{notification.body}</span>
          {destination ? (
            <span className="text-primary mt-1 block text-xs font-semibold">
              {t(destination.actionKey)}
            </span>
          ) : null}
        </span>
        {isUnread ? (
          <span
            role="img"
            aria-label={t('inbox.unread_aria')}
            className="bg-primary mt-1.5 size-2.5 shrink-0 rounded-full"
          />
        ) : null}
      </button>
    </li>
  );
}
