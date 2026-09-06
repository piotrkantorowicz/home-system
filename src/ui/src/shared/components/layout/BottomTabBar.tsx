import { cn } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';
import { NavLink, useLocation } from 'react-router-dom';

import { getActiveModule, getMobileNavItems } from './navModel';

/**
 * Mobile nav (< 768px) — scoped to the active module only (see BUILD_REVIEW.md
 * #fix-nav: a bar flattening every module's items into one row is a soup once
 * a second module exists). Switching modules on mobile happens from the
 * header's module-switcher trigger.
 */
export function BottomTabBar() {
  const { t } = useTranslation();
  const location = useLocation();
  const mod = getActiveModule(location.pathname);
  if (!mod) return null;

  const items = getMobileNavItems(t, mod);

  return (
    <nav
      aria-label={t(mod.translationKey)}
      className="border-border bg-card fixed inset-x-0 bottom-0 z-20 flex items-stretch gap-1 overflow-x-auto border-t px-2 pt-1.5 pb-[max(0.75rem,env(safe-area-inset-bottom))] md:hidden"
    >
      {items.map((item) => (
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
          <item.icon className="size-[21px]" strokeWidth={1.9} />
          <span className="text-[10px] leading-none font-semibold">{item.label}</span>
          {item.Badge ? (
            <span className="absolute top-0.5 right-2">
              <item.Badge />
            </span>
          ) : null}
        </NavLink>
      ))}
    </nav>
  );
}
