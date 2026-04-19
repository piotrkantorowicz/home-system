import { Button, ThemeToggle, LanguageSwitcher } from '@shared/components/ui';
import { User, LogOut, Menu } from 'lucide-react';
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
      <div className="flex items-center gap-3">
        <LanguageSwitcher />
        <ThemeToggle />
        <div className="bg-border mx-1 h-6 w-px" />
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2.5">
            <div className="from-primary/20 to-accent/30 rounded-full bg-gradient-to-br p-2">
              <User className="text-primary h-4 w-4" />
            </div>
            <div className="hidden sm:block">
              <p className="text-sm leading-tight font-medium">{displayName}</p>
              {profile.email && (
                <p className="text-muted-foreground text-xs leading-tight">{profile.email}</p>
              )}
            </div>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={handleLogout}
            className="text-muted-foreground hover:text-destructive"
          >
            <LogOut className="mr-2 h-4 w-4" />
            <span className="hidden sm:inline">{t('common.logout')}</span>
          </Button>
        </div>
      </div>
    </header>
  );
}
