import { UserProfileDropdown } from '@shared/components/ui';
import { useTheme } from '@shared/context/ThemeContext';
import { cn } from '@shared/lib/utils';
import { Moon, Sun } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';
import { NavLink } from 'react-router-dom';

import { getRailNavItems } from './navModel';

export function IconRail() {
  const { t } = useTranslation();
  const { resolvedTheme, setTheme } = useTheme();
  const auth = useAuth();

  const items = getRailNavItems(t);
  const profile = auth.user?.profile;
  const displayName = profile?.name ?? profile?.preferred_username ?? profile?.email ?? 'User';

  const handleLogout = () => {
    void auth.signoutRedirect();
  };

  return (
    <nav
      aria-label="Primary"
      className="border-border bg-card sticky top-0 flex h-screen w-[76px] flex-none flex-col items-center gap-1.5 self-start border-r px-0 py-[18px]"
    >
      {/* Logo mark */}
      <NavLink
        to="/"
        end
        aria-label="HomeSystem"
        className="bg-primary text-primary-foreground mb-4 grid size-10 place-items-center rounded-[13px] text-[17px] font-bold shadow-sm"
      >
        H
      </NavLink>

      {items.map((item) => {
        const Icon = item.icon;
        return (
          <NavLink
            key={item.href}
            to={item.href}
            end={item.end}
            className={({ isActive }) =>
              cn(
                'relative flex w-14 flex-col items-center gap-[5px] rounded-[14px] py-[9px] transition-colors duration-150 ease-out',
                isActive
                  ? 'bg-accent text-accent-foreground'
                  : 'text-text-2 hover:bg-muted hover:text-foreground',
              )
            }
          >
            <Icon className="size-5" strokeWidth={1.9} />
            <span className="text-[10px] leading-tight font-semibold">{item.label}</span>
            {item.Badge ? (
              <span className="absolute top-1 right-1">
                <item.Badge />
              </span>
            ) : null}
          </NavLink>
        );
      })}

      <div className="flex-1" />

      {/* Light / dark toggle — the `system` option lives in Preferences. */}
      <button
        type="button"
        onClick={() => {
          setTheme(resolvedTheme === 'dark' ? 'light' : 'dark');
        }}
        aria-label={resolvedTheme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
        className="border-border bg-secondary text-text-2 hover:border-border-strong hover:text-foreground grid size-11 place-items-center rounded-[14px] border transition-colors duration-150"
      >
        {resolvedTheme === 'dark' ? (
          <Sun className="size-[18px]" strokeWidth={1.9} />
        ) : (
          <Moon className="size-[18px]" strokeWidth={1.9} />
        )}
      </button>

      <div className="mt-2">
        <UserProfileDropdown
          compact
          displayName={displayName}
          email={profile?.email ?? undefined}
          onLogout={handleLogout}
        />
      </div>
    </nav>
  );
}
