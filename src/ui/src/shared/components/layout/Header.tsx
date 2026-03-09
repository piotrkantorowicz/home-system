import { User, LogOut } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { useTranslation } from 'react-i18next';
import { Button, ThemeToggle, LanguageSwitcher } from '@shared/components/ui';

export function Header() {
  const { t } = useTranslation();
  const auth = useAuth();

  if (!auth.isAuthenticated || !auth.user) {
    return null;
  }

  const { profile } = auth.user;
  const displayName = profile?.name || profile?.preferred_username || profile?.email || 'User';

  const handleLogout = () => {
    auth.signoutRedirect();
  };

  return (
    <header className="flex h-16 items-center justify-between border-b glass px-6">
      <div className="flex items-center gap-4">
        <h2 className="text-base font-medium text-foreground/80">
          {t('common.welcome_back', { name: displayName })}
        </h2>
      </div>
      <div className="flex items-center gap-3">
        <LanguageSwitcher />
        <ThemeToggle />
        <div className="h-6 w-px bg-border mx-1" />
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2.5">
            <div className="rounded-full bg-gradient-to-br from-primary/20 to-accent/30 p-2">
              <User className="h-4 w-4 text-primary" />
            </div>
            <div className="hidden sm:block">
              <p className="text-sm font-medium leading-tight">{displayName}</p>
              {profile?.email && (
                <p className="text-xs text-muted-foreground leading-tight">{profile.email}</p>
              )}
            </div>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={handleLogout}
            className="text-muted-foreground hover:text-destructive"
          >
            <LogOut className="h-4 w-4 mr-2" />
            <span className="hidden sm:inline">{t('common.logout')}</span>
          </Button>
        </div>
      </div>
    </header>
  );
}
