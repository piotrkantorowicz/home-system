import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Home, ChevronDown } from 'lucide-react';
import { cn } from '@shared/lib/utils';
import { getModules } from '@shared/lib/module-registry';

export function Sidebar() {
  const { t } = useTranslation();
  const location = useLocation();
  const modules = getModules();

  return (
    <div className="flex h-full w-64 flex-col border-r glass">
      {/* Brand */}
      <div className="flex h-16 items-center border-b border-border/50 px-6">
        <Link to="/" className="text-xl font-bold tracking-tight gradient-text">
          HomeSystem
        </Link>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto px-3 py-5 space-y-1">
        {/* System-level home */}
        <Link
          to="/"
          className={cn(
            'flex items-center gap-3 rounded-lg px-3 py-2.5 text-[0.9rem] font-medium transition-all duration-200',
            location.pathname === '/'
              ? 'bg-primary/10 text-primary shadow-sm'
              : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground'
          )}
        >
          <Home
            className={cn(
              'h-[18px] w-[18px] transition-colors duration-200',
              location.pathname === '/' ? 'text-primary' : 'text-muted-foreground'
            )}
          />
          {t('common.dashboard')}
        </Link>

        {/* Module sections */}
        {modules.length > 0 && (
          <div className="pt-3 mt-3 border-t border-border/30 space-y-1">
            <p className="px-3 pb-1 text-[0.7rem] font-semibold uppercase tracking-widest text-muted-foreground/50">
              {t('common.modules')}
            </p>
            {modules.map((mod) => (
              <ModuleSection key={mod.name} mod={mod} location={location} t={t} />
            ))}
          </div>
        )}
      </nav>

      {/* Footer */}
      <div className="border-t border-border/50 p-4">
        <p className="text-xs text-muted-foreground/60 font-medium">v1.0.0</p>
      </div>
    </div>
  );
}

interface ModuleSectionProps {
  mod: ReturnType<typeof getModules>[number];
  location: ReturnType<typeof import('react-router-dom').useLocation>;
  t: ReturnType<typeof import('react-i18next').useTranslation>['t'];
}

function ModuleSection({ mod, location, t }: ModuleSectionProps) {
  const isModuleActive = location.pathname.startsWith(mod.basePath);
  const [isExpanded, setIsExpanded] = useState(isModuleActive);

  const ModIcon = mod.icon;

  // Filter out the module "dashboard" nav item — the module header itself acts as the entry
  const childNavItems = mod.navItems.filter((item) => item.href !== mod.basePath);

  return (
    <div>
      {/* Module header — clickable to expand/collapse */}
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className={cn(
          'flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-[0.9rem] font-medium transition-all duration-200',
          isModuleActive
            ? 'text-primary'
            : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground'
        )}
      >
        <ModIcon
          className={cn(
            'h-[18px] w-[18px] transition-colors duration-200',
            isModuleActive ? 'text-primary' : 'text-muted-foreground'
          )}
        />
        <span className="flex-1 text-left">{t(mod.translationKey)}</span>
        <ChevronDown
          className={cn(
            'h-4 w-4 text-muted-foreground transition-transform duration-200',
            isExpanded && 'rotate-180'
          )}
        />
      </button>

      {/* Child nav items */}
      {isExpanded && (
        <div className="ml-4 mt-0.5 space-y-0.5 border-l border-border/30 pl-3">
          {/* Module home link */}
          <Link
            to={mod.basePath}
            className={cn(
              'flex items-center gap-2.5 rounded-lg px-3 py-2 text-[0.85rem] font-medium transition-all duration-200',
              location.pathname === mod.basePath
                ? 'bg-primary/10 text-primary shadow-sm'
                : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground'
            )}
          >
            <Home className="h-4 w-4" />
            {t('common.dashboard')}
          </Link>

          {childNavItems.map((item) => {
            const isActive =
              location.pathname === item.href ||
              (location.pathname.startsWith(item.href) && item.href !== mod.basePath);
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                to={item.href}
                className={cn(
                  'flex items-center gap-2.5 rounded-lg px-3 py-2 text-[0.85rem] font-medium transition-all duration-200',
                  isActive
                    ? 'bg-primary/10 text-primary shadow-sm'
                    : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground'
                )}
              >
                <Icon
                  className={cn(
                    'h-4 w-4 transition-colors duration-200',
                    isActive ? 'text-primary' : 'text-muted-foreground'
                  )}
                />
                {t(item.translationKey)}
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}
