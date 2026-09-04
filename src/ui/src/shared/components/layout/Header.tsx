import { NotificationsPanel } from '@modules/notifications/components/NotificationsPanel';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

export function Header() {
  const auth = useAuth();
  const { t } = useTranslation();

  if (!auth.isAuthenticated || !auth.user) {
    return null;
  }

  return (
    <header
      style={{ background: 'var(--glass-bg)' }}
      className="border-border sticky top-0 z-10 flex flex-wrap items-center gap-4 border-b px-4 py-3.5 backdrop-blur-[14px] md:gap-5 md:px-8"
    >
      <div className="flex items-baseline gap-2.5">
        <span className="text-[17px] font-bold tracking-tight">HomeSystem</span>
        <span className="text-muted-foreground hidden text-xs sm:inline">
          {t('common.app_tagline')}
        </span>
      </div>

      <div className="ml-auto flex items-center gap-2.5">
        <NotificationsPanel />
      </div>
    </header>
  );
}
