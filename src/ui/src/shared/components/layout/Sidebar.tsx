import { useUserRoles } from '@shared/auth/useUserRoles';
import { UserProfileDropdown } from '@shared/components/ui';
import { useHouseholdRole } from '@shared/context/HouseholdRoleContext';
import { useModuleLabels } from '@shared/context/ModuleLabelsContext';
import { cn } from '@shared/lib/utils';
import { ChevronsUpDown, Search, SlidersHorizontal } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';
import { useLocation } from 'react-router-dom';

import { OPEN_COMMAND_PALETTE_EVENT } from './CommandPalette';
import { GatedNavLink } from './GatedNavLink';
import { ModuleSwitcher } from './ModuleSwitcher';
import {
  getFooterDestinations,
  getSectionGroups,
  readLastModule,
  resolveShellModule,
} from './navModel';

import type { ReactNode } from 'react';

const itemClass = (isActive: boolean) =>
  cn(
    'rounded-10px text-13px focus-visible:ring-primary flex items-center gap-2.5 px-2.5 py-2 font-semibold transition-colors focus-visible:ring-2 focus-visible:outline-none',
    isActive
      ? 'bg-accent text-accent-foreground'
      : 'text-text-2 hover:bg-muted hover:text-foreground',
  );

/**
 * The desktop (md+) 240px sidebar: module switcher, search, the active module's nav, then the
 * footer destinations (Household, Notifications, Admin, Settings) and the user. Below md the
 * {@link BottomTabBar} and the mobile header take over.
 */
export function Sidebar() {
  const { t } = useTranslation();
  const auth = useAuth();
  const location = useLocation();
  const labels = useModuleLabels();
  const roles = useUserRoles();
  const householdRole = useHouseholdRole();

  const mod = resolveShellModule(location.pathname, roles, householdRole, readLastModule());
  const footer = getFooterDestinations(t, labels, roles, householdRole);
  const { groups, pinned } = mod
    ? getSectionGroups(t, mod, householdRole)
    : { groups: [], pinned: [] };
  const settings = pinned[0];

  const profile = auth.user?.profile;
  const displayName = profile?.name ?? profile?.preferred_username ?? profile?.email ?? 'User';
  const moduleLabel = mod ? (labels[mod.name] ?? t(mod.translationKey)) : t('common.modules');
  const ModuleIcon = mod?.icon;

  const link = (
    href: string,
    className: (active: boolean) => string,
    end: boolean,
    content: ReactNode,
  ) => (
    <GatedNavLink key={href} href={href} end={end} className={className}>
      {content}
    </GatedNavLink>
  );

  return (
    <aside className="border-border bg-secondary sticky top-0 hidden h-screen w-[240px] flex-none flex-col self-start border-r px-3 py-4 md:flex">
      <ModuleSwitcher activeName={mod?.name}>
        <button
          type="button"
          aria-label={t('common.switch_module')}
          className="hover:bg-muted rounded-10px focus-visible:ring-primary mb-3 flex w-full items-center gap-2.5 px-1.5 py-1.5 text-left transition-colors focus-visible:ring-2 focus-visible:outline-none"
        >
          <span className="bg-primary text-primary-foreground size-34px grid flex-none place-items-center rounded-xl text-[15px] font-bold">
            {ModuleIcon ? <ModuleIcon className="size-[18px]" strokeWidth={1.9} /> : 'H'}
          </span>
          <span className="min-w-0 flex-1">
            <span className="text-13-5px block truncate font-bold">{moduleLabel}</span>
            <span className="text-text-2 text-11px block truncate">
              {labels.household ?? 'HomeSystem'}
            </span>
          </span>
          <ChevronsUpDown className="text-text-2 size-4 flex-none" />
        </button>
      </ModuleSwitcher>

      <button
        type="button"
        onClick={() => {
          window.dispatchEvent(new Event(OPEN_COMMAND_PALETTE_EVENT));
        }}
        className="border-border bg-card text-muted-foreground focus-visible:ring-primary h-36px rounded-10px mb-3 flex w-full items-center gap-2 border px-2.5 focus-visible:ring-2 focus-visible:outline-none"
      >
        <Search className="size-[15px] shrink-0" strokeWidth={2} />
        <span className="text-13px flex-1 text-left">{t('common.search_everything')}</span>
        <kbd className="bg-muted text-muted-foreground rounded-6px text-10px px-1.5 py-0.5 font-bold">
          ⌘K
        </kbd>
      </button>

      <nav aria-label={moduleLabel} className="flex flex-1 flex-col gap-0.5 overflow-y-auto">
        {groups.map((group, gi) => (
          <div key={group.label ?? `g${String(gi)}`} className={gi > 0 ? 'mt-3.5' : undefined}>
            {group.label ? (
              <div className="text-text-2 text-10px px-2.5 pb-1 font-bold tracking-wider uppercase">
                {group.label}
              </div>
            ) : null}
            {group.items.map((item) =>
              link(
                item.href,
                itemClass,
                item.end,
                <>
                  <item.icon className="size-4 shrink-0" strokeWidth={1.9} />
                  <span className="min-w-0 flex-1 truncate">{item.label}</span>
                  {item.Badge ? <item.Badge /> : null}
                </>,
              ),
            )}
          </div>
        ))}
      </nav>

      <nav
        aria-label={t('common.shell.destinations')}
        className="border-border mt-2 flex flex-col gap-0.5 border-t pt-2"
      >
        {footer.map((item) =>
          link(
            item.href,
            itemClass,
            item.end,
            <>
              <item.icon className="size-4 shrink-0" strokeWidth={1.9} />
              <span className="min-w-0 flex-1 truncate">{item.label}</span>
              {item.Badge ? <item.Badge /> : null}
            </>,
          ),
        )}
        {settings
          ? link(
              settings.href,
              itemClass,
              settings.end,
              <>
                <SlidersHorizontal className="size-4 shrink-0" strokeWidth={1.9} />
                <span className="min-w-0 flex-1 truncate">{t('common.shell.settings')}</span>
              </>,
            )
          : null}
      </nav>

      <div className="border-border mt-2 flex items-center gap-2.5 border-t px-1.5 pt-3">
        <UserProfileDropdown
          compact
          displayName={displayName}
          email={profile?.email ?? undefined}
          onLogout={() => {
            void auth.signoutRedirect();
          }}
        />
        <span className="text-13px min-w-0 flex-1 truncate font-semibold">{displayName}</span>
      </div>
    </aside>
  );
}
