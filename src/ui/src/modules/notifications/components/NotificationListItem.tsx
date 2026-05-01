import { cn } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';

import { formatTimeAgo } from '../utils/timeAgo';
import { getTypeMeta } from '../utils/typeMeta';

import type { NotificationDto } from '../api/hooks/useNotifications';

export interface NotificationListItemProps {
  notification: NotificationDto;
  onActivate: (id: string) => void;
  now: Date;
}

export function NotificationListItem({ notification, onActivate, now }: NotificationListItemProps) {
  const { t, i18n } = useTranslation('notifications');
  const meta = getTypeMeta(notification.type ?? '');
  const Icon = meta.icon;
  const isUnread = notification.readAt === null || notification.readAt === undefined;

  function handleActivate() {
    if (isUnread && notification.id) onActivate(notification.id);
  }

  return (
    <li>
      <button
        type="button"
        onClick={handleActivate}
        aria-pressed={!isUnread}
        aria-label={isUnread ? t('inbox.mark_read_aria') : undefined}
        className={cn(
          'group border-border bg-surface flex w-full items-start gap-3 rounded-md border p-4 text-left transition-colors',
          'focus-visible:ring-primary focus-visible:ring-2 focus-visible:outline-none',
          'hover:bg-surface-alt',
          isUnread && 'border-l-primary bg-surface-alt/40 border-l-4',
        )}
      >
        <Icon aria-hidden className="text-text-muted mt-0.5 size-5 shrink-0" />
        <div className="min-w-0 flex-1">
          <div className="flex items-baseline justify-between gap-2">
            <p
              className={cn(
                'truncate text-sm',
                isUnread ? 'text-text font-semibold' : 'text-text-muted font-medium',
              )}
            >
              {notification.title}
            </p>
            <span className="text-text-muted shrink-0 text-xs">
              {notification.createdAt
                ? formatTimeAgo(notification.createdAt, i18n.language, now)
                : ''}
            </span>
          </div>
          <p className="text-text-muted mt-1 line-clamp-1 text-sm">{notification.body}</p>
          <p className="text-text-muted mt-1 text-xs">{t(meta.labelKey)}</p>
        </div>
      </button>
    </li>
  );
}
