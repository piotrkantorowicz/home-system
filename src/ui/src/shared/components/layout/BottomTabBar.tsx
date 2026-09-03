import { cn } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';

import { getRailNavItems } from './navModel';

export function BottomTabBar() {
  // Subscribe to language changes so labels stay current.
  useTranslation();
  const items = getRailNavItems();

  return (
    <nav
      aria-label="Primary"
      className="border-border bg-card fixed inset-x-0 bottom-0 z-20 flex items-stretch gap-1 overflow-x-auto border-t px-2 pt-1.5 pb-[max(0.75rem,env(safe-area-inset-bottom))] md:hidden"
    >
      {items.map((item) => {
        const Icon = item.icon;
        return (
          <NavLink
            key={item.href}
            to={item.href}
            end={item.end}
            className={({ isActive }) =>
              cn(
                'relative flex min-w-[4.25rem] flex-1 flex-col items-center gap-1 rounded-[12px] px-1 py-1.5 transition-colors',
                isActive ? 'text-primary' : 'text-muted-foreground',
              )
            }
          >
            <Icon className="size-[21px]" strokeWidth={1.9} />
            <span className="text-[10px] leading-none font-semibold">{item.labelEn}</span>
            {item.Badge ? (
              <span className="absolute top-0.5 right-2">
                <item.Badge />
              </span>
            ) : null}
          </NavLink>
        );
      })}
    </nav>
  );
}
