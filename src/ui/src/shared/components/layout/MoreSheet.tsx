import { useUserRoles } from '@shared/auth/useUserRoles';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  UserProfileDropdown,
} from '@shared/components/ui';
import { useHouseholdRole } from '@shared/context/HouseholdRoleContext';
import { useModuleLabels } from '@shared/context/ModuleLabelsContext';
import { useNavigationAccess } from '@shared/context/NavigationAccessContext';
import { cn } from '@shared/lib/utils';
import { Check, SlidersHorizontal } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';
import { useNavigate } from 'react-router-dom';

import { GatedNavLink } from './GatedNavLink';
import { getFooterDestinations, getModuleTiles, getSectionGroups } from './navModel';

import type { MobileNav } from './navModel';
import type { AppModule } from '@shared/lib/module-registry';
import type { RefObject } from 'react';

const itemClass = (isActive: boolean) =>
  cn(
    'rounded-10px text-body focus-visible:ring-primary flex min-h-11 items-center gap-3 px-3 font-semibold transition-colors focus-visible:ring-2 focus-visible:outline-none',
    isActive
      ? 'bg-accent text-accent-foreground'
      : 'text-text-2 hover:bg-muted hover:text-foreground',
  );

const headingClass = 'text-text-2 text-label px-3 pt-4 pb-1 font-bold tracking-wider uppercase';

export interface MoreSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  mod: AppModule;
  nav: MobileNav;
  /** The More button; Radix only restores focus to a Trigger, so it is returned here explicitly. */
  returnFocusTo: RefObject<HTMLElement | null>;
}

/**
 * The phone "More" sheet: the module destinations that did not fit the tab bar, the module
 * switcher, and the footer group (Household, Notifications, Admin, Settings) plus the account menu.
 * A Radix dialog — focus is trapped while open and Escape closes it.
 */
export function MoreSheet({ open, onOpenChange, mod, nav, returnFocusTo }: MoreSheetProps) {
  const { t } = useTranslation();
  const auth = useAuth();
  const navigate = useNavigate();
  const access = useNavigationAccess();
  const labels = useModuleLabels();
  const roles = useUserRoles();
  const householdRole = useHouseholdRole();

  const tiles = getModuleTiles(t, labels, roles, householdRole);
  const footer = getFooterDestinations(t, labels, roles, householdRole);
  const settings = getSectionGroups(t, mod, householdRole).pinned[0];
  const profile = auth.user?.profile;
  const displayName = profile?.name ?? profile?.preferred_username ?? profile?.email ?? 'User';
  const close = () => {
    onOpenChange(false);
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="bottom"
        aria-describedby={undefined}
        onCloseAutoFocus={(event) => {
          event.preventDefault();
          returnFocusTo.current?.focus();
        }}
        className="border-border bg-card border-t px-3 pt-2 pb-[max(1rem,env(safe-area-inset-bottom))]"
      >
        <SheetHeader className="p-3 pb-0">
          <SheetTitle>{t('common.shell.more')}</SheetTitle>
          <SheetDescription className="sr-only">{t('common.shell.more_hint')}</SheetDescription>
        </SheetHeader>

        {nav.moreGroups.map((group, gi) => (
          <nav
            key={group.label ?? `g${String(gi)}`}
            aria-label={group.label ?? t(mod.translationKey)}
            className="flex flex-col gap-0.5"
          >
            {group.label ? <div className={headingClass}>{group.label}</div> : null}
            {group.items.map((item) => (
              <GatedNavLink
                key={item.href}
                href={item.href}
                end={item.end}
                className={itemClass}
                onNavigate={close}
              >
                <item.icon className="size-[18px] shrink-0" strokeWidth={1.9} />
                <span className="min-w-0 flex-1 truncate">{item.label}</span>
                {item.Badge ? <item.Badge /> : null}
              </GatedNavLink>
            ))}
          </nav>
        ))}

        <div className={headingClass}>{t('common.modules')}</div>
        {access.reason ? (
          <p className="text-muted-foreground px-3 pb-2 text-xs">{access.reason}</p>
        ) : null}
        <div className="flex flex-col gap-0.5">
          {tiles.map((tile) => (
            <button
              key={tile.name}
              type="button"
              disabled={!access.canNavigate(tile.basePath)}
              aria-current={tile.name === mod.name ? 'true' : undefined}
              onClick={() => {
                close();
                void navigate(tile.basePath);
              }}
              className={cn(
                itemClass(tile.name === mod.name),
                'w-full text-left disabled:opacity-40',
              )}
            >
              <tile.icon className="size-[18px] shrink-0" strokeWidth={1.9} />
              <span className="min-w-0 flex-1">
                <span className="block truncate">{tile.label}</span>
                {tile.description ? (
                  <span className="text-muted-foreground text-label block truncate font-normal">
                    {tile.description}
                  </span>
                ) : null}
              </span>
              {tile.name === mod.name ? (
                <Check className="size-4 flex-none" aria-label={t('common.current')} />
              ) : null}
            </button>
          ))}
        </div>

        <nav
          aria-label={t('common.shell.destinations')}
          className="border-border mt-3 flex flex-col gap-0.5 border-t pt-2"
        >
          {footer.map((item) => (
            <GatedNavLink
              key={item.href}
              href={item.href}
              end={item.end}
              className={itemClass}
              onNavigate={close}
            >
              <item.icon className="size-[18px] shrink-0" strokeWidth={1.9} />
              <span className="min-w-0 flex-1 truncate">{item.label}</span>
              {item.Badge ? <item.Badge /> : null}
            </GatedNavLink>
          ))}
          {settings ? (
            <GatedNavLink
              href={settings.href}
              end={settings.end}
              className={itemClass}
              onNavigate={close}
            >
              <SlidersHorizontal className="size-[18px] shrink-0" strokeWidth={1.9} />
              <span className="min-w-0 flex-1 truncate">{t('common.shell.settings')}</span>
            </GatedNavLink>
          ) : null}
        </nav>

        <div className="border-border mt-2 flex items-center gap-3 border-t px-3 pt-3">
          <UserProfileDropdown
            compact
            displayName={displayName}
            email={profile?.email ?? undefined}
            onLogout={() => {
              void auth.signoutRedirect();
            }}
          />
          <span className="text-body min-w-0 flex-1 truncate font-semibold">{displayName}</span>
        </div>
      </SheetContent>
    </Sheet>
  );
}
