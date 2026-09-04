import { LAST_MODULE_STORAGE_KEY } from '@shared/components/layout/navModel';
import { EmptyState } from '@shared/components/ui';
import { getModules } from '@shared/lib/module-registry';
import { LayoutGrid } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Navigate } from 'react-router-dom';

/**
 * `/` resolves rather than renders (BUILD_REVIEW.md #fix-home) — the old
 * "Welcome back" launcher page cost a click and taught nothing. One module
 * installed -> its basePath. Several -> the last one actually visited
 * (persisted by AppShell). None -> the only case that still needs a page.
 */
export default function RootRedirect() {
  const { t } = useTranslation();
  const modules = getModules();

  if (modules.length === 0) {
    return (
      <div className="mx-auto max-w-md px-4 py-16">
        <EmptyState
          icon={LayoutGrid}
          title={t('common.no_modules_title')}
          description={t('common.no_modules_body')}
        />
      </div>
    );
  }

  if (modules.length === 1) {
    return <Navigate to={modules[0]?.basePath ?? '/'} replace />;
  }

  let lastModuleName: string | null = null;
  try {
    lastModuleName = window.localStorage.getItem(LAST_MODULE_STORAGE_KEY);
  } catch {
    // storage unavailable — fall back to the first registered module
  }

  const target = modules.find((m) => m.name === lastModuleName) ?? modules[0];
  return <Navigate to={target?.basePath ?? '/'} replace />;
}
