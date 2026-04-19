import { getModules } from '@shared/lib/module-registry';
import { cn } from '@shared/lib/utils';
import { type TFunction } from 'i18next';
import { Home, ChevronDown } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, type Location } from 'react-router-dom';

interface SidebarProps {
  onClose?: () => void;
}

export function Sidebar({ onClose }: SidebarProps) {
  const { t } = useTranslation();
  const location = useLocation();
  const modules = getModules();

  return (
    <div className="glass flex h-full w-64 flex-col border-r">
      {/* Brand */}
      <div className="border-border/50 flex h-16 items-center border-b px-6">
        <Link to="/" onClick={onClose} className="gradient-text text-xl font-bold tracking-tight">
          HomeSystem
        </Link>
      </div>

      {/* Navigation */}
      <nav className="flex-1 space-y-1 overflow-y-auto px-3 py-5">
        {/* System-level home */}
        <Link
          to="/"
          onClick={onClose}
          aria-current={location.pathname === '/' ? 'page' : undefined}
          className={cn(
            'flex items-center gap-3 rounded-lg px-3 py-2.5 text-[0.9rem] font-medium transition-all duration-200',
            location.pathname === '/'
              ? 'bg-primary/20 text-primary border-primary border-l-2 font-semibold shadow-sm'
              : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground',
          )}
        >
          <Home
            className={cn(
              'h-[18px] w-[18px] transition-colors duration-200',
              location.pathname === '/' ? 'text-primary' : 'text-muted-foreground',
            )}
          />
          {t('common.dashboard')}
        </Link>

        {/* Module sections */}
        {modules.length > 0 && (
          <div className="border-border/30 mt-3 space-y-1 border-t pt-3">
            <p className="text-muted-foreground/50 px-3 pb-1 text-[0.7rem] font-semibold tracking-widest uppercase">
              {t('common.modules')}
            </p>
            {modules.map((mod) => (
              <ModuleSection
                key={mod.name}
                mod={mod}
                location={location}
                t={t}
                {...(onClose !== undefined && { onClose })}
              />
            ))}
          </div>
        )}
      </nav>

      {/* Footer */}
      <div className="border-border/50 border-t p-4">
        <p className="text-muted-foreground/60 text-xs font-medium">v1.0.0</p>
      </div>
    </div>
  );
}

interface ModuleSectionProps {
  mod: ReturnType<typeof getModules>[number];
  location: Location;
  t: TFunction;
  onClose?: () => void;
}

function ModuleSection({ mod, location, t, onClose }: ModuleSectionProps) {
  const isModuleActive = location.pathname.startsWith(mod.basePath);
  const [isExpanded, setIsExpanded] = useState(isModuleActive);

  const ModIcon = mod.icon;

  // Filter out the module "dashboard" nav item — the module header itself acts as the entry
  const childNavItems = mod.navItems.filter((item) => item.href !== mod.basePath);

  return (
    <div>
      {/* Module header — clickable to expand/collapse */}
      <button
        onClick={() => {
          setIsExpanded(!isExpanded);
        }}
        className={cn(
          'flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-[0.9rem] font-medium transition-all duration-200',
          isModuleActive
            ? 'text-primary'
            : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground',
        )}
      >
        <ModIcon
          className={cn(
            'h-[18px] w-[18px] transition-colors duration-200',
            isModuleActive ? 'text-primary' : 'text-muted-foreground',
          )}
        />
        <span className="flex-1 text-left">{t(mod.translationKey)}</span>
        <ChevronDown
          className={cn(
            'text-muted-foreground h-4 w-4 transition-transform duration-200',
            isExpanded && 'rotate-180',
          )}
        />
      </button>

      {/* Child nav items */}
      {isExpanded && (
        <div className="border-border/30 mt-0.5 ml-4 space-y-0.5 border-l pl-3">
          {/* Module home link */}
          <Link
            to={mod.basePath}
            onClick={onClose}
            aria-current={location.pathname === mod.basePath ? 'page' : undefined}
            className={cn(
              'flex items-center gap-2.5 rounded-lg px-3 py-2 text-[0.85rem] font-medium transition-all duration-200',
              location.pathname === mod.basePath
                ? 'bg-primary/20 text-primary border-primary border-l-2 font-semibold shadow-sm'
                : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground',
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
                onClick={onClose}
                aria-current={isActive ? 'page' : undefined}
                className={cn(
                  'flex items-center gap-2.5 rounded-lg px-3 py-2 text-[0.85rem] font-medium transition-all duration-200',
                  isActive
                    ? 'bg-primary/20 text-primary border-primary border-l-2 font-semibold shadow-sm'
                    : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground',
                )}
              >
                <Icon
                  className={cn(
                    'h-4 w-4 transition-colors duration-200',
                    isActive ? 'text-primary' : 'text-muted-foreground',
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
