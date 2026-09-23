import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from '@shared/components/ui/Dialog';
import { useModuleLabels } from '@shared/context/ModuleLabelsContext';
import { useNavigationAccess } from '@shared/context/NavigationAccessContext';
import { getModules } from '@shared/lib/module-registry';
import { Search } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import type { TFunction } from 'i18next';
import type { LucideIcon } from 'lucide-react';

/** Fired by any trigger (module switcher, ⌘K) to open the palette without prop-drilling. */
export const OPEN_COMMAND_PALETTE_EVENT = 'home-system:open-command-palette';

interface Destination {
  moduleLabel: string;
  href: string;
  label: string;
  Icon: LucideIcon;
}

function collectDestinations(
  labels: Readonly<Record<string, string>>,
  t: TFunction,
): Destination[] {
  const out: Destination[] = [];
  for (const mod of getModules()) {
    const moduleLabel = labels[mod.name] ?? t(mod.translationKey);
    for (const nav of mod.navItems) {
      out.push({ moduleLabel, href: nav.href, label: t(nav.translationKey), Icon: nav.icon });
    }
  }
  return out;
}

function filterDestinations(destinations: Destination[], query: string): Destination[] {
  const q = query.trim().toLowerCase();
  if (!q) return destinations;
  return destinations.filter(
    (d) => d.label.toLowerCase().includes(q) || d.moduleLabel.toLowerCase().includes(q),
  );
}

/**
 * Cross-module "go to" palette. Flattens every registered module's nav items
 * into one filterable list — the replacement for the header search field,
 * which searched nothing. Opens on ⌘K / Ctrl+K or the custom open event.
 */
export function CommandPalette() {
  const { t } = useTranslation();
  const labels = useModuleLabels();
  const access = useNavigationAccess();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');

  useEffect(() => {
    function onOpenEvent() {
      setOpen(true);
    }
    function onKeyDown(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        setOpen(true);
      }
    }
    window.addEventListener(OPEN_COMMAND_PALETTE_EVENT, onOpenEvent);
    window.addEventListener('keydown', onKeyDown);
    return () => {
      window.removeEventListener(OPEN_COMMAND_PALETTE_EVENT, onOpenEvent);
      window.removeEventListener('keydown', onKeyDown);
    };
  }, []);

  function handleOpenChange(next: boolean) {
    setOpen(next);
    if (!next) setQuery('');
  }

  const destinations = collectDestinations(labels, t);
  const filtered = filterDestinations(destinations, query);

  function go(href: string) {
    if (!access.canNavigate(href)) return;
    setOpen(false);
    void navigate(href);
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="top-[18%] max-w-[520px] translate-y-0 gap-0 overflow-hidden p-0">
        <DialogTitle className="sr-only">{t('common.search_everything')}</DialogTitle>
        <DialogDescription className="sr-only">{t('common.search_everything')}</DialogDescription>

        <div className="border-border flex items-center gap-2.5 border-b px-4">
          <Search className="text-muted-foreground size-15px shrink-0" strokeWidth={2} />
          <input
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
            }}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && filtered[0]) go(filtered[0].href);
            }}
            placeholder={t('common.search_everything')}
            aria-label={t('common.search_everything')}
            className="text-foreground placeholder:text-muted-foreground h-52px text-14px w-full bg-transparent outline-none"
          />
        </div>

        {access.reason && (
          <p className="text-muted-foreground px-4 pt-3 text-sm">{access.reason}</p>
        )}
        <ul role="listbox" className="max-h-[360px] overflow-y-auto p-2">
          {filtered.length === 0 ? (
            <li className="text-muted-foreground text-13px px-3 py-6 text-center">
              {t('common.no_matches')}
            </li>
          ) : (
            filtered.map((d) => (
              <li key={d.href}>
                <button
                  type="button"
                  role="option"
                  disabled={!access.canNavigate(d.href)}
                  aria-selected={false}
                  onClick={() => {
                    go(d.href);
                  }}
                  className="hover:bg-accent hover:text-accent-foreground focus-visible:bg-accent rounded-10px text-13px flex w-full items-center gap-3 px-3 py-2.5 text-left font-semibold outline-none disabled:cursor-not-allowed disabled:opacity-40"
                >
                  <d.Icon className="text-muted-foreground size-4 shrink-0" strokeWidth={1.9} />
                  <span className="min-w-0 flex-1 truncate">{d.label}</span>
                  <span className="text-muted-foreground text-11px shrink-0 font-medium">
                    {d.moduleLabel}
                  </span>
                </button>
              </li>
            ))
          )}
        </ul>
      </DialogContent>
    </Dialog>
  );
}
