import { NotificationsPanel } from '@modules/notifications/components/NotificationsPanel';
import { UserProfileDropdown } from '@shared/components/ui';
import { Search } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';
import { useNavigate } from 'react-router-dom';

export function Header() {
  const auth = useAuth();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [query, setQuery] = useState('');

  if (!auth.isAuthenticated || !auth.user) {
    return null;
  }

  const { profile } = auth.user;
  const displayName = profile.name ?? profile.preferred_username ?? profile.email ?? 'User';

  const handleLogout = () => {
    void auth.signoutRedirect();
  };

  const handleSearch = (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    void navigate('/diet-planner/products');
  };

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

      <form
        onSubmit={handleSearch}
        role="search"
        className="border-border bg-card text-muted-foreground focus-within:ring-primary hidden h-[38px] max-w-[380px] flex-1 items-center gap-2.5 rounded-[12px] border px-3 focus-within:ring-2 md:flex"
      >
        <Search className="size-[15px] shrink-0" strokeWidth={2} />
        <input
          type="search"
          value={query}
          onChange={(event) => {
            setQuery(event.target.value);
          }}
          placeholder={t('common.search_placeholder')}
          aria-label={t('common.search_placeholder')}
          className="text-foreground placeholder:text-muted-foreground w-full bg-transparent text-[13px] outline-none"
        />
      </form>

      <div className="ml-auto flex items-center gap-2.5">
        <NotificationsPanel />
        <UserProfileDropdown
          compact
          displayName={displayName}
          email={profile.email ?? undefined}
          onLogout={handleLogout}
        />
      </div>
    </header>
  );
}
