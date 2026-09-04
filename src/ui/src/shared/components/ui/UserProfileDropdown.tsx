import { cn, getInitials } from '@shared/lib/utils';
import { User, Bell, LogOut, SlidersHorizontal } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from './DropdownMenu';
import { LanguageSwitcher } from './LanguageSwitcher';
import { ThemeToggle } from './ThemeToggle';

export interface UserProfileDropdownProps {
  displayName: string;
  email?: string | undefined;
  onLogout: () => void;
  /** Render just a 34px initials avatar as the trigger (icon rail / condensed header). */
  compact?: boolean;
}

const dietPlannerLinks = [
  { to: '/diet-planner/profile', icon: User, translationKey: 'common.profile' },
] as const;

const settingsLinks = [
  {
    to: '/diet-planner/preferences',
    icon: SlidersHorizontal,
    translationKey: 'common.preferences',
  },
  {
    to: '/notifications',
    icon: Bell,
    translationKey: 'common.notifications',
  },
] as const;

export function UserProfileDropdown({
  displayName,
  email,
  onLogout,
  compact = false,
}: UserProfileDropdownProps) {
  const { t } = useTranslation();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        {compact ? (
          <button
            aria-label={t('common.user_menu')}
            className="focus-visible:ring-primary grid size-[34px] place-items-center rounded-[11px] text-[12px] font-bold text-white transition-[filter] duration-150 hover:brightness-110 focus-visible:ring-2 focus-visible:outline-none"
            style={{ background: 'var(--gradient-avatar)' }}
          >
            {getInitials(displayName)}
          </button>
        ) : (
          <button
            className="hover:bg-accent/60 focus-visible:ring-primary flex items-center gap-2.5 rounded-lg p-1.5 transition-colors focus-visible:ring-2 focus-visible:outline-none"
            aria-label={t('common.user_menu')}
          >
            <div className="from-primary/20 to-accent/30 rounded-full bg-gradient-to-br p-2">
              <User className="text-primary h-4 w-4" />
            </div>
            <div className="hidden text-left sm:block">
              <p className="text-sm leading-tight font-medium">{displayName}</p>
              {email && <p className="text-muted-foreground text-xs leading-tight">{email}</p>}
            </div>
          </button>
        )}
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-64">
        {/* User info header */}
        <DropdownMenuLabel>
          <div className="flex items-center gap-3 py-1">
            <div className="from-primary/20 to-accent/30 rounded-full bg-gradient-to-br p-2">
              <User className="text-primary h-4 w-4" />
            </div>
            <div>
              <p className="text-sm font-medium">{displayName}</p>
              {email && <p className="text-muted-foreground text-xs">{email}</p>}
            </div>
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />

        {/* Diet Planner group */}
        <DropdownMenuGroup>
          <DropdownMenuLabel className="text-muted-foreground px-2 py-1 text-xs font-semibold">
            {t('common.diet_planner')}
          </DropdownMenuLabel>
          {dietPlannerLinks.map((link) => {
            const Icon = link.icon;
            return (
              <DropdownMenuItem key={link.to} asChild>
                <Link to={link.to} className={cn('flex items-center gap-2')}>
                  <Icon className="h-4 w-4" />
                  {t(link.translationKey)}
                </Link>
              </DropdownMenuItem>
            );
          })}
        </DropdownMenuGroup>

        <DropdownMenuSeparator />

        {/* Settings group */}
        <DropdownMenuGroup>
          <DropdownMenuLabel className="text-muted-foreground px-2 py-1 text-xs font-semibold">
            {t('common.settings')}
          </DropdownMenuLabel>
          {settingsLinks.map((link) => {
            const Icon = link.icon;
            return (
              <DropdownMenuItem key={link.to} asChild>
                <Link to={link.to} className={cn('flex items-center gap-2')}>
                  <Icon className="h-4 w-4" />
                  {t(link.translationKey)}
                </Link>
              </DropdownMenuItem>
            );
          })}
        </DropdownMenuGroup>

        <DropdownMenuSeparator />

        {/* Preferences */}
        <DropdownMenuGroup>
          <div className="flex items-center justify-between px-3 py-1.5">
            <LanguageSwitcher />
            <ThemeToggle />
          </div>
        </DropdownMenuGroup>

        <DropdownMenuSeparator />

        {/* Logout */}
        <DropdownMenuItem onClick={onLogout} className="text-destructive focus:text-destructive">
          <LogOut className="h-4 w-4" />
          {t('common.logout')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
