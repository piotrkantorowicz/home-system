import { cn } from '@shared/lib/utils';
import { ChevronsLeft, ChevronsRight } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink, useLocation } from 'react-router-dom';

import { getActiveModule, getSectionGroups } from './navModel';

const STORAGE_KEY = 'home-system-nav-collapsed';

function readCollapsed(): boolean {
  if (typeof window === 'undefined') return false;
  try {
    return window.localStorage.getItem(STORAGE_KEY) === '1';
  } catch {
    return false;
  }
}

/**
 * 216px section panel — the second tier of the two-tier nav. Names the
 * destinations inside the module the {@link ModuleRail} has already picked.
 * Collapses to an icon-only 56px strip; the choice persists.
 */
export function SectionPanel() {
  const { t } = useTranslation();
  const location = useLocation();
  const [collapsed, setCollapsed] = useState(readCollapsed);

  useEffect(() => {
    try {
      window.localStorage.setItem(STORAGE_KEY, collapsed ? '1' : '0');
    } catch {
      // storage unavailable — collapse state stays in-memory only
    }
  }, [collapsed]);

  const mod = getActiveModule(location.pathname);
  if (!mod) return null;

  const { groups, pinned } = getSectionGroups(t, mod);
  const Icon = mod.icon;

  return (
    <div
      className={cn(
        'border-border bg-card sticky top-0 flex h-screen flex-none flex-col self-start border-r py-4 transition-[width] duration-150',
        collapsed ? 'w-14 items-center px-2' : 'w-[216px] px-3',
      )}
    >
      {collapsed ? (
        <button
          type="button"
          onClick={() => {
            setCollapsed(false);
          }}
          aria-label={t('common.expand_nav')}
          title={t('common.expand_nav')}
          className="text-text-2 hover:bg-muted hover:text-foreground mb-3 grid size-8 place-items-center rounded-[9px] transition-colors"
        >
          <ChevronsRight className="size-4" />
        </button>
      ) : (
        <div className="mb-3 flex items-center gap-2 px-1.5 pb-2">
          <span className="bg-accent text-accent-foreground grid size-7 flex-none place-items-center rounded-[9px]">
            <Icon className="size-[15px]" />
          </span>
          <span className="min-w-0 flex-1 truncate text-[13.5px] font-bold">{t(mod.translationKey)}</span>
          <button
            type="button"
            onClick={() => {
              setCollapsed(true);
            }}
            aria-label={t('common.collapse_nav')}
            title={t('common.collapse_nav')}
            className="text-text-2 hover:bg-muted hover:text-foreground grid size-7 flex-none place-items-center rounded-[8px] transition-colors"
          >
            <ChevronsLeft className="size-4" />
          </button>
        </div>
      )}

      <nav aria-label={t(mod.translationKey)} className="flex flex-1 flex-col gap-0.5 overflow-y-auto">
        {groups.map((group, gi) => (
          <div key={group.label ?? `g${String(gi)}`} className={gi > 0 ? 'mt-3.5' : undefined}>
            {group.label && !collapsed ? (
              <div className="text-text-2 px-2 pb-1 text-[10px] font-bold tracking-wider uppercase">
                {group.label}
              </div>
            ) : null}
            {group.items.map((item) => (
              <NavLink
                key={item.href}
                to={item.href}
                end={item.end}
                title={collapsed ? item.label : undefined}
                aria-label={collapsed ? item.label : undefined}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-2.5 rounded-[10px] py-2 text-[13px] font-semibold transition-colors',
                    collapsed ? 'justify-center px-0' : 'px-2.5',
                    isActive
                      ? 'bg-accent text-accent-foreground'
                      : 'text-text-2 hover:bg-muted hover:text-foreground',
                  )
                }
              >
                <item.icon className="size-4 shrink-0" strokeWidth={1.9} />
                {collapsed ? null : <span className="min-w-0 flex-1 truncate">{item.label}</span>}
                {item.Badge && !collapsed ? <item.Badge /> : null}
              </NavLink>
            ))}
          </div>
        ))}
      </nav>

      {pinned.length > 0 ? (
        <div className="border-border mt-2 flex flex-col gap-0.5 border-t pt-2">
          {pinned.map((item) => (
            <NavLink
              key={item.href}
              to={item.href}
              end={item.end}
              title={collapsed ? item.label : undefined}
              aria-label={collapsed ? item.label : undefined}
              className={({ isActive }) =>
                cn(
                  'text-text-2 flex items-center gap-2.5 rounded-[10px] py-2 text-[12.5px] font-semibold transition-colors',
                  collapsed ? 'justify-center px-0' : 'px-2.5',
                  isActive ? 'text-foreground' : 'hover:text-foreground',
                )
              }
            >
              <item.icon className="size-4 shrink-0" strokeWidth={1.9} />
              {collapsed ? null : <span className="min-w-0 flex-1 truncate">{item.label}</span>}
            </NavLink>
          ))}
        </div>
      ) : null}
    </div>
  );
}
