import { Checkbox } from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';

import { formatTimeAgo } from '../utils/timeAgo';
import { getTypeMeta } from '../utils/typeMeta';

import type { NotificationDto } from '../api/hooks/useNotifications';

export interface NotificationListItemProps {
  notification: NotificationDto;
  onActivate: (id: string) => void;
  now: Date;
  selected?: boolean;
  onToggleSelect?: (id: string, next: boolean) => void;
  showSelect?: boolean;
}

export function NotificationListItem({
  notification,
  onActivate,
  now,
  selected = false,
  onToggleSelect,
  showSelect = true,
}: NotificationListItemProps) {
  const { t, i18n } = useTranslation('notifications');
  const meta = getTypeMeta(notification.type);
  const Icon = meta.icon;
  const isUnread = !notification.readAt;
  const id = notification.id;

  function handleActivate() {
    if (isUnread) onActivate(id);
  }

  return (
    <li
      className={cn(
        'border-border bg-surface flex items-start gap-3 rounded-md border p-4 transition-opacity',
        isUnread ? 'border-l-primary border-l-4' : 'opacity-60',
      )}
    >
      {showSelect && onToggleSelect && (
        <Checkbox
          className="mt-1"
          checked={selected}
          disabled={!isUnread}
          aria-label={t('inbox.select_row_aria')}
          onChange={(e) => {
            onToggleSelect(id, e.target.checked);
          }}
        />
      )}
      <button
        type="button"
        onClick={handleActivate}
        disabled={!isUnread}
        aria-pressed={!isUnread}
        aria-label={isUnread ? t('inbox.mark_read_aria') : undefined}
        className={cn(
          'flex flex-1 items-start gap-3 text-left transition-colors',
          'focus-visible:ring-primary rounded-sm focus-visible:ring-2 focus-visible:outline-none',
          isUnread && 'hover:opacity-80',
        )}
      >
        <Icon aria-hidden className="text-text-muted mt-0.5 size-5 shrink-0" />
        <div className="min-w-0 flex-1">
          <div className="flex items-baseline justify-between gap-2">
            <p
              className={cn(
                'truncate text-sm',
                isUnread ? 'text-text font-semibold' : 'text-text-muted font-medium line-through',
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
