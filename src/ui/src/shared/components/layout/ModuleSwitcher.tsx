import { useUserRoles } from '@shared/auth/useUserRoles';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@shared/components/ui';
import { useHouseholdRole } from '@shared/context/HouseholdRoleContext';
import { useModuleLabels } from '@shared/context/ModuleLabelsContext';
import { useNavigationAccess } from '@shared/context/NavigationAccessContext';
import { Check, Search } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { OPEN_COMMAND_PALETTE_EVENT } from './CommandPalette';
import { getFooterDestinations, getModuleTiles } from './navModel';

import type { ReactNode } from 'react';

export interface ModuleSwitcherProps {
  children: ReactNode;
  /** `name` of the module to mark as current. */
  activeName?: string | undefined;
}

/**
 * Dropdown listing every product module (name, one-line summary, check on the current one), a
 * link to Household and "Search everything" (⌘K). Triggered from the sidebar's top block and the
 * mobile header.
 */
export function ModuleSwitcher({ children, activeName }: ModuleSwitcherProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const labels = useModuleLabels();
  const roles = useUserRoles();
  const householdRole = useHouseholdRole();
  const tiles = getModuleTiles(t, labels, roles, householdRole);
  const household = getFooterDestinations(t, labels, roles, householdRole).find(
    (d) => d.name === 'household',
  );
  const access = useNavigationAccess();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{children}</DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-[280px]">
        <DropdownMenuLabel className="text-muted-foreground text-10px font-bold tracking-wide uppercase">
          {t('common.modules')}
        </DropdownMenuLabel>
        {access.reason && (
          <p className="text-muted-foreground px-2 pb-2 text-xs">{access.reason}</p>
        )}
        {tiles.map((tile) => {
          const Icon = tile.icon;
          const isActive = tile.name === activeName;
          return (
            <DropdownMenuItem
              key={tile.name}
              disabled={!access.canNavigate(tile.basePath)}
              onSelect={() => {
                void navigate(tile.basePath);
              }}
              className={isActive ? 'bg-accent text-accent-foreground' : undefined}
            >
              <span className="bg-accent text-accent-foreground size-30px rounded-10px grid shrink-0 place-items-center">
                <Icon className="size-4" />
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate font-semibold">{tile.label}</span>
                {tile.description ? (
                  <span className="text-muted-foreground text-11px block truncate">
                    {tile.description}
                  </span>
                ) : null}
              </span>
              {isActive ? (
                <Check className="size-4 flex-none" aria-label={t('common.current')} />
              ) : null}
            </DropdownMenuItem>
          );
        })}
        <DropdownMenuSeparator />
        {household ? (
          <DropdownMenuItem
            disabled={!access.canNavigate(household.href)}
            onSelect={() => {
              void navigate(household.href);
            }}
          >
            <household.icon className="size-4" />
            <span className="flex-1 font-semibold">{household.label}</span>
          </DropdownMenuItem>
        ) : null}
        <DropdownMenuItem
          onSelect={() => {
            window.dispatchEvent(new Event(OPEN_COMMAND_PALETTE_EVENT));
          }}
        >
          <Search className="size-4" />
          <span className="flex-1 font-semibold">{t('common.search_everything')}</span>
          <span className="bg-muted text-muted-foreground rounded-6px text-10px px-1.5 py-0.5 font-bold">
            ⌘K
          </span>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
