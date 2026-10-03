import { useUserRoles } from '@shared/auth/useUserRoles';
import { useHouseholdRole } from '@shared/context/HouseholdRoleContext';
import { useModuleLabels } from '@shared/context/ModuleLabelsContext';
import { cn } from '@shared/lib/utils';
import { Ellipsis } from 'lucide-react';
import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation } from 'react-router-dom';

import { GatedNavLink } from './GatedNavLink';
import { MoreSheet } from './MoreSheet';
import {
  getFooterDestinations,
  getMobileNav,
  readLastModule,
  resolveShellModule,
} from './navModel';

const isUnder = (pathname: string, href: string) => {
  const path = href.split('?')[0] ?? href;
  return pathname === path || pathname.startsWith(`${path}/`);
};

/**
 * Phone nav (< 768px): the module's tabs (at most five slots with the optional raised action and
 * More), scoped to the product module you are in — footer pages (Household, Notifications…) keep
 * the module you came from. Everything else, the module switcher and the footer group live in the
 * {@link MoreSheet}.
 */
export function BottomTabBar() {
  const { t } = useTranslation();
  const location = useLocation();
  const labels = useModuleLabels();
  const roles = useUserRoles();
  const householdRole = useHouseholdRole();
  const [moreOpen, setMoreOpen] = useState(false);
  const moreRef = useRef<HTMLButtonElement>(null);

  const mod = resolveShellModule(location.pathname, roles, householdRole, readLastModule());
  if (!mod) return null;

  const nav = getMobileNav(t, mod, householdRole);
  const moreActive =
    nav.moreGroups.some((g) => g.items.some((i) => isUnder(location.pathname, i.href))) ||
    getFooterDestinations(t, labels, roles, householdRole).some((d) =>
      isUnder(location.pathname, d.href),
    );

  const tabClass = (isActive: boolean) =>
    cn(
      'rounded-12px text-label relative flex min-h-11 min-w-0 flex-1 flex-col items-center justify-center gap-1 px-1 py-1.5 font-semibold transition-colors',
      isActive ? 'text-primary' : 'text-muted-foreground',
    );
  const tab = (item: (typeof nav.tabs)[number]) => (
    <GatedNavLink key={item.href} href={item.href} end={item.end} className={tabClass}>
      <item.icon className="size-[21px]" strokeWidth={1.9} />
      <span className="max-w-full truncate leading-none">{item.label}</span>
      {item.Badge ? (
        <span className="absolute top-0.5 right-2">
          <item.Badge />
        </span>
      ) : null}
    </GatedNavLink>
  );

  return (
    <>
      <nav
        aria-label={labels[mod.name] ?? t(mod.translationKey)}
        className="border-border bg-card fixed inset-x-0 bottom-0 z-20 flex items-stretch gap-1 border-t px-2 pt-1.5 pb-[max(0.75rem,env(safe-area-inset-bottom))] md:hidden"
      >
        {nav.tabs.slice(0, 2).map(tab)}
        {nav.action ? (
          <GatedNavLink
            href={nav.action.href}
            end
            className={() =>
              'bg-primary text-primary-foreground focus-visible:ring-primary -mt-5 grid size-14 flex-none place-items-center self-start rounded-full shadow-lg focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none'
            }
          >
            <nav.action.icon className="size-6" strokeWidth={2.2} />
            <span className="sr-only">{nav.action.label}</span>
          </GatedNavLink>
        ) : null}
        {nav.tabs.slice(2).map(tab)}
        <button
          ref={moreRef}
          type="button"
          aria-haspopup="dialog"
          aria-expanded={moreOpen}
          aria-current={moreActive ? 'page' : undefined}
          onClick={() => {
            setMoreOpen(true);
          }}
          className={tabClass(moreActive)}
        >
          <Ellipsis className="size-[21px]" strokeWidth={1.9} />
          <span className="leading-none">{t('common.shell.more')}</span>
        </button>
      </nav>
      <MoreSheet
        open={moreOpen}
        onOpenChange={setMoreOpen}
        mod={mod}
        nav={nav}
        returnFocusTo={moreRef}
      />
    </>
  );
}
