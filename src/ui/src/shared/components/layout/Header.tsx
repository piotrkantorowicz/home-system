import { UserProfileDropdown } from '@shared/components/ui';
import { Menu } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

interface HeaderProps {
  onOpenSidebar?: () => void;
}

export function Header({ onOpenSidebar }: HeaderProps) {
  const { t } = useTranslation();
  const auth = useAuth();

  if (!auth.isAuthenticated || !auth.user) {
    return null;
  }

  const { profile } = auth.user;
  const displayName = profile.name ?? profile.preferred_username ?? profile.email ?? 'User';

  const handleLogout = () => {
    void auth.signoutRedirect();
  };

  return (
    <header className="glass flex h-16 items-center justify-between border-b px-6">
      <div className="flex items-center gap-4">
        <button
          className="text-muted-foreground hover:text-foreground -ml-1 rounded-md p-1.5 transition-colors lg:hidden"
          onClick={onOpenSidebar}
          aria-label="Open navigation"
        >
          <Menu className="h-5 w-5" />
        </button>
        <h2 className="text-foreground/80 text-base font-medium">
          {t('common.welcome_back', { name: displayName })}
        </h2>
      </div>
      <UserProfileDropdown
        displayName={displayName}
        email={profile.email ?? undefined}
        onLogout={handleLogout}
      />
    </header>
  );
}
