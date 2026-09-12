import { NotificationsPanel } from '@modules/notifications/components/NotificationsPanel';
import { useModuleLabels } from '@shared/context/ModuleLabelsContext';
import { Search } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';
import { useLocation } from 'react-router-dom';

import { OPEN_COMMAND_PALETTE_EVENT } from './CommandPalette';
import { ModuleSwitcher } from './ModuleSwitcher';
import { getActiveModule } from './navModel';

export function Header() {
  const auth = useAuth();
  const { t } = useTranslation();
  const labels = useModuleLabels();
  const location = useLocation();

  if (!auth.isAuthenticated || !auth.user) {
    return null;
  }

  const activeModule = getActiveModule(location.pathname);

  return (
    <header
      style={{ background: 'var(--glass-bg)' }}
      className="border-border sticky top-0 z-10 flex flex-wrap items-center gap-4 border-b px-4 py-3.5 backdrop-blur-[14px] md:gap-5 md:px-8"
    >
      <ModuleSwitcher>
        <button
          type="button"
          aria-label={t('common.switch_module')}
          className="hover:bg-muted -ml-1.5 flex items-baseline gap-2.5 rounded-[10px] px-1.5 py-1 transition-colors"
        >
          <span className="text-[17px] font-bold tracking-tight">HomeSystem</span>
          <span className="text-muted-foreground hidden text-xs sm:inline">
            {activeModule
              ? (labels[activeModule.name] ?? t(activeModule.translationKey))
              : t('common.app_tagline')}
          </span>
        </button>
      </ModuleSwitcher>

      <button
        type="button"
        onClick={() => {
          window.dispatchEvent(new Event(OPEN_COMMAND_PALETTE_EVENT));
        }}
        className="border-border bg-card text-muted-foreground focus-visible:ring-primary hidden h-[38px] max-w-[380px] flex-1 items-center gap-2.5 rounded-[12px] border px-3 focus-visible:ring-2 md:flex"
      >
        <Search className="size-[15px] shrink-0" strokeWidth={2} />
        <span className="flex-1 text-left text-[13px]">{t('common.search_everything')}</span>
        <span className="bg-muted text-muted-foreground rounded-[6px] px-1.5 py-0.5 text-[10px] font-bold">
          ⌘K
        </span>
      </button>

      <div className="ml-auto flex items-center gap-2.5">
        <NotificationsPanel />
      </div>
    </header>
  );
}
