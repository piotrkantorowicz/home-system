import { Bell } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { UnreadBadge } from './UnreadBadge';

export function NotificationsBell() {
  const { t } = useTranslation();

  return (
    <Link
      to="/notifications"
      aria-label={t('common.notifications')}
      className="text-muted-foreground hover:text-foreground hover:bg-accent/60 focus-visible:ring-primary relative inline-flex h-9 w-9 items-center justify-center rounded-md transition-colors focus-visible:ring-2 focus-visible:outline-none"
    >
      <Bell className="h-5 w-5" />
      <span className="pointer-events-none absolute -top-1 -right-1">
        <UnreadBadge />
      </span>
    </Link>
  );
}
