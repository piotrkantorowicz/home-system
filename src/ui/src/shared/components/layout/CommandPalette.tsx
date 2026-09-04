import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from '@shared/components/ui/Dialog';
import { getModules } from '@shared/lib/module-registry';
import { Search } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import type { LucideIcon } from 'lucide-react';

/** Fired by any trigger (module switcher, ⌘K) to open the palette without prop-drilling. */
export const OPEN_COMMAND_PALETTE_EVENT = 'home-system:open-command-palette';

interface Destination {
  moduleLabel: string;
  href: string;
  label: string;
  Icon: LucideIcon;
}

/**
 * Cross-module "go to" palette. Flattens every registered module's nav items
 * into one filterable list — the replacement for the header search field,
 * which searched nothing. Opens on ⌘K / Ctrl+K or the custom open event.
 */
export function CommandPalette() {
  const { t } = useTranslation();
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

  const destinations = useMemo(() => {
    const out: Destination[] = [];
    for (const mod of getModules()) {
      const moduleLabel = t(mod.translationKey);
      for (const nav of mod.navItems) {
        out.push({ moduleLabel, href: nav.href, label: t(nav.translationKey), Icon: nav.icon });
      }
    }
    return out;
  }, [t]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return destinations;
    return destinations.filter(
      (d) => d.label.toLowerCase().includes(q) || d.moduleLabel.toLowerCase().includes(q),
    );
  }, [destinations, query]);

  function go(href: string) {
    setOpen(false);
    void navigate(href);
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="top-[18%] max-w-[520px] translate-y-0 gap-0 overflow-hidden p-0">
        <DialogTitle className="sr-only">{t('common.search_everything')}</DialogTitle>
        <DialogDescription className="sr-only">{t('common.search_everything')}</DialogDescription>

        <div className="border-border flex items-center gap-2.5 border-b px-4">
          <Search className="text-muted-foreground size-[15px] shrink-0" strokeWidth={2} />
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
            className="text-foreground placeholder:text-muted-foreground h-[52px] w-full bg-transparent text-[14px] outline-none"
          />
        </div>

        <ul role="listbox" className="max-h-[360px] overflow-y-auto p-2">
          {filtered.length === 0 ? (
            <li className="text-muted-foreground px-3 py-6 text-center text-[13px]">
              {t('common.no_matches')}
            </li>
          ) : (
            filtered.map((d) => (
              <li key={d.href}>
                <button
                  type="button"
                  role="option"
                  aria-selected={false}
                  onClick={() => {
                    go(d.href);
                  }}
                  className="hover:bg-accent hover:text-accent-foreground focus-visible:bg-accent flex w-full items-center gap-3 rounded-[10px] px-3 py-2.5 text-left text-[13px] font-semibold outline-none"
                >
                  <d.Icon className="text-muted-foreground size-4 shrink-0" strokeWidth={1.9} />
                  <span className="min-w-0 flex-1 truncate">{d.label}</span>
                  <span className="text-muted-foreground shrink-0 text-[11px] font-medium">
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
