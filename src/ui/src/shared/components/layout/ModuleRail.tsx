import { UserProfileDropdown } from '@shared/components/ui';
import { useModuleLabels } from '@shared/context/ModuleLabelsContext';
import { useNavigationAccess } from '@shared/context/NavigationAccessContext';
import { useTheme } from '@shared/context/ThemeContext';
import { cn } from '@shared/lib/utils';
import { Moon, Sun } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';
import { useLocation, useNavigate } from 'react-router-dom';

import { ModuleSwitcher } from './ModuleSwitcher';
import { getModuleTiles } from './navModel';

/**
 * 64px module rail — the top tier of the two-tier nav. Picks the *product*
 * (which registered module); the {@link SectionPanel} beside it picks the
 * destination within it. See docs/design/redesign/BUILD_REVIEW.md #fix-nav.
 */
export function ModuleRail() {
  const { t } = useTranslation();
  const { resolvedTheme, setTheme } = useTheme();
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const access = useNavigationAccess();

  const tiles = getModuleTiles(t, useModuleLabels());
  const profile = auth.user?.profile;
  const displayName = profile?.name ?? profile?.preferred_username ?? profile?.email ?? 'User';

  const handleLogout = () => {
    void auth.signoutRedirect();
  };

  return (
    <nav
      aria-label="Modules"
      className="border-border bg-secondary sticky top-0 flex h-screen w-16 flex-none flex-col items-center gap-2 self-start border-r py-[14px]"
    >
      <ModuleSwitcher>
        <button
          type="button"
          aria-label={t('common.switch_module')}
          className="bg-primary text-primary-foreground mb-2.5 grid size-[38px] place-items-center rounded-xl text-[16px] font-bold"
        >
          H
        </button>
      </ModuleSwitcher>

      {tiles.map((tile) => {
        const Icon = tile.icon;
        const isActive =
          location.pathname === tile.basePath || location.pathname.startsWith(`${tile.basePath}/`);
        return (
          <button
            key={tile.name}
            type="button"
            title={access.canNavigate(tile.basePath) ? tile.label : access.reason}
            disabled={!access.canNavigate(tile.basePath)}
            aria-label={tile.label}
            aria-current={isActive ? 'page' : undefined}
            onClick={() => {
              void navigate(tile.basePath);
            }}
            className={cn(
              'relative grid size-[42px] flex-none place-items-center rounded-[13px] transition-colors duration-150 disabled:cursor-not-allowed disabled:opacity-40',
              isActive
                ? 'bg-card border-border-strong text-primary border shadow-sm'
                : 'text-text-2 hover:bg-card/60',
            )}
          >
            {isActive ? (
              <span className="bg-primary absolute top-1/2 -left-4 h-[22px] w-[3px] -translate-y-1/2 rounded-full" />
            ) : null}
            <Icon className="size-[19px]" strokeWidth={1.9} />
          </button>
        );
      })}

      <div className="flex-1" />

      <button
        type="button"
        onClick={() => {
          setTheme(resolvedTheme === 'dark' ? 'light' : 'dark');
        }}
        aria-label={resolvedTheme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
        className="border-border bg-card text-text-2 hover:border-border-strong hover:text-foreground grid size-[38px] place-items-center rounded-xl border transition-colors duration-150"
      >
        {resolvedTheme === 'dark' ? (
          <Sun className="size-4" strokeWidth={1.9} />
        ) : (
          <Moon className="size-4" strokeWidth={1.9} />
        )}
      </button>

      <div className="mt-1.5">
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
