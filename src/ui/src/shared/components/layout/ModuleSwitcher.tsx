import { useUserRoles } from '@shared/auth/useUserRoles';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@shared/components/ui';
import { useModuleLabels } from '@shared/context/ModuleLabelsContext';
import { useNavigationAccess } from '@shared/context/NavigationAccessContext';
import { Search } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';

import { OPEN_COMMAND_PALETTE_EVENT } from './CommandPalette';
import { getModuleTiles } from './navModel';

import type { ReactNode } from 'react';

export interface ModuleSwitcherProps {
  children: ReactNode;
}

/**
 * Dropdown listing every registered module plus "Search everything" (⌘K).
 * Triggered from the module rail's "H" mark and the mobile header.
 */
export function ModuleSwitcher({ children }: ModuleSwitcherProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const tiles = getModuleTiles(t, useModuleLabels(), useUserRoles());
  const access = useNavigationAccess();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{children}</DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-[240px]">
        <DropdownMenuLabel className="text-muted-foreground text-10px font-bold tracking-wide uppercase">
          {t('common.modules')}
        </DropdownMenuLabel>
        {access.reason && (
          <p className="text-muted-foreground px-2 pb-2 text-xs">{access.reason}</p>
        )}
        {tiles.map((tile) => {
          const Icon = tile.icon;
          const isActive =
            location.pathname === tile.basePath ||
            location.pathname.startsWith(`${tile.basePath}/`);
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
              <span className="min-w-0 flex-1 truncate font-semibold">{tile.label}</span>
            </DropdownMenuItem>
          );
        })}
        <DropdownMenuSeparator />
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
