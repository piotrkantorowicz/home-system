import { NotificationsPanel } from '@modules/notifications/components/NotificationsPanel';
import { useModuleLabels } from '@shared/context/ModuleLabelsContext';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';
import { useLocation } from 'react-router-dom';

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
      className="border-border sticky top-0 z-10 flex flex-wrap items-center gap-4 border-b px-4 py-3.5 backdrop-blur-[14px] md:hidden"
    >
      <ModuleSwitcher activeName={activeModule?.name}>
        <button
          type="button"
          aria-label={t('common.switch_module')}
          className="hover:bg-muted rounded-10px -ml-1.5 flex items-baseline gap-2.5 px-1.5 py-1 transition-colors"
        >
          <span className="text-17px font-bold tracking-tight">HomeSystem</span>
          <span className="text-muted-foreground hidden text-xs sm:inline">
            {activeModule
              ? (labels[activeModule.name] ?? t(activeModule.translationKey))
              : t('common.app_tagline')}
          </span>
        </button>
      </ModuleSwitcher>

      <div className="ml-auto flex items-center gap-2.5">
        <NotificationsPanel />
      </div>
    </header>
  );
}
