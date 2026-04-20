import { cn } from '@shared/lib/utils';
import { User, Target, Clock, Bell, LogOut } from 'lucide-react';
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
}

const settingsLinks = [
  { to: '/diet-planner/profile', icon: User, translationKey: 'common.profile' },
  { to: '/diet-planner/goals', icon: Target, translationKey: 'common.goals' },
  { to: '/diet-planner/meal-schedule', icon: Clock, translationKey: 'meal_schedule.nav' },
  {
    to: '/diet-planner/notification-preferences',
    icon: Bell,
    translationKey: 'notifications.nav',
  },
] as const;

export function UserProfileDropdown({ displayName, email, onLogout }: UserProfileDropdownProps) {
  const { t } = useTranslation();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
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
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-64">
        {/* User info (visible on mobile where the trigger hides name) */}
        <DropdownMenuLabel className="sm:hidden">
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
        <DropdownMenuSeparator className="sm:hidden" />

        {/* Settings links */}
        <DropdownMenuGroup>
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
