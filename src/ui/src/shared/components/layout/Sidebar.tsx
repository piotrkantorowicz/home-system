import * as PopoverPrimitive from '@radix-ui/react-popover';
import { getModules } from '@shared/lib/module-registry';
import { cn } from '@shared/lib/utils';
import { type TFunction } from 'i18next';
import { ChevronDown, Home, PanelLeftClose, PanelLeftOpen } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, type Location } from 'react-router-dom';

interface SidebarProps {
  onClose?: () => void;
  collapsed?: boolean;
  onToggleCollapsed?: () => void;
}

export function Sidebar({ onClose, collapsed = false, onToggleCollapsed }: SidebarProps) {
  const { t } = useTranslation();
  const location = useLocation();
  const modules = getModules();

  return (
    <div
      className={cn(
        'glass flex h-full flex-col border-r transition-[width] duration-200',
        collapsed ? 'w-16' : 'w-64',
      )}
    >
      {/* Brand row */}
      {!collapsed && (
        <div className="border-border/50 flex h-16 items-center border-b px-6">
          <Link to="/" onClick={onClose} className="gradient-text text-xl font-bold tracking-tight">
            HomeSystem
          </Link>
        </div>
      )}
      {collapsed && <div className="border-border/50 h-16 border-b" />}

      {/* Navigation */}
      <nav className={cn('flex-1 space-y-1 overflow-y-auto py-5', collapsed ? 'px-2' : 'px-3')}>
        {/* System-level home */}
        <Link
          to="/"
          onClick={onClose}
          aria-current={location.pathname === '/' ? 'page' : undefined}
          title={collapsed ? t('common.dashboard') : undefined}
          className={cn(
            'flex items-center rounded-lg text-[0.9rem] font-medium transition-all duration-200',
            collapsed ? 'h-10 justify-center px-0' : 'gap-3 px-3 py-2.5',
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
          {!collapsed && t('common.dashboard')}
        </Link>

        {/* Module sections */}
        {modules.length > 0 && (
          <div className="border-border/30 mt-3 space-y-1 border-t pt-3">
            {!collapsed && (
              <p className="text-muted-foreground/50 px-3 pb-1 text-[0.7rem] font-semibold tracking-widest uppercase">
                {t('common.modules')}
              </p>
            )}
            {modules.map((mod) => (
              <ModuleSection
                key={mod.name}
                mod={mod}
                location={location}
                t={t}
                collapsed={collapsed}
                {...(onClose !== undefined && { onClose })}
              />
            ))}
          </div>
        )}
      </nav>

      {/* Footer — collapse toggle (always) + version (when expanded) */}
      <div
        className={cn(
          'border-border/50 flex items-center border-t',
          collapsed ? 'justify-center p-2' : 'justify-between p-4',
        )}
      >
        {!collapsed && <p className="text-muted-foreground/60 text-xs font-medium">v1.0.0</p>}
        {onToggleCollapsed && (
          <button
            type="button"
            onClick={onToggleCollapsed}
            aria-label={collapsed ? t('common.sidebar.expand') : t('common.sidebar.collapse')}
            title={collapsed ? t('common.sidebar.expand') : t('common.sidebar.collapse')}
            className="text-muted-foreground hover:text-foreground hover:bg-accent/60 focus-visible:ring-primary rounded-md p-1.5 transition-colors focus-visible:ring-2 focus-visible:outline-none"
          >
            {collapsed ? (
              <PanelLeftOpen className="h-4 w-4" />
            ) : (
              <PanelLeftClose className="h-4 w-4" />
            )}
          </button>
        )}
      </div>
    </div>
  );
}

interface ModuleSectionProps {
  mod: ReturnType<typeof getModules>[number];
  location: Location;
  t: TFunction;
  collapsed: boolean;
  onClose?: () => void;
}

function ModuleSection({ mod, location, t, collapsed, onClose }: ModuleSectionProps) {
  const isModuleActive = location.pathname.startsWith(mod.basePath);
  const [isExpanded, setIsExpanded] = useState(isModuleActive);

  if (collapsed) {
    return (
      <CollapsedModuleSection
        mod={mod}
        location={location}
        t={t}
        isModuleActive={isModuleActive}
        {...(onClose !== undefined && { onClose })}
      />
    );
  }

  const ModIcon = mod.icon;

  return (
    <div>
      <button
        type="button"
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

      {isExpanded && (
        <div className="border-border/30 mt-0.5 ml-4 space-y-0.5 border-l pl-3">
          {mod.navItems.map((item) => (
            <SubNavLink
              key={item.href}
              item={item}
              modBasePath={mod.basePath}
              location={location}
              t={t}
              {...(onClose !== undefined && { onClose })}
            />
          ))}
        </div>
      )}
    </div>
  );
}

interface CollapsedModuleSectionProps {
  mod: ReturnType<typeof getModules>[number];
  location: Location;
  t: TFunction;
  isModuleActive: boolean;
  onClose?: () => void;
}

function CollapsedModuleSection({
  mod,
  location,
  t,
  isModuleActive,
  onClose,
}: CollapsedModuleSectionProps) {
  const [open, setOpen] = useState(false);
  const closeTimeoutRef = useRef<number | null>(null);

  const cancelClose = () => {
    if (closeTimeoutRef.current !== null) {
      window.clearTimeout(closeTimeoutRef.current);
      closeTimeoutRef.current = null;
    }
  };

  const scheduleClose = () => {
    cancelClose();
    closeTimeoutRef.current = window.setTimeout(() => {
      setOpen(false);
    }, 200);
  };

  useEffect(() => {
    return () => {
      cancelClose();
    };
  }, []);

  const ModIcon = mod.icon;

  return (
    <PopoverPrimitive.Root open={open} onOpenChange={setOpen}>
      <PopoverPrimitive.Trigger asChild>
        <Link
          to={mod.basePath}
          onClick={() => {
            onClose?.();
            setOpen(false);
          }}
          onMouseEnter={() => {
            cancelClose();
            setOpen(true);
          }}
          onMouseLeave={scheduleClose}
          onFocus={() => {
            setOpen(true);
          }}
          aria-label={t(mod.translationKey)}
          title={t(mod.translationKey)}
          className={cn(
            'flex h-10 w-full items-center justify-center rounded-lg transition-all duration-200',
            isModuleActive
              ? 'bg-primary/20 text-primary'
              : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground',
          )}
        >
          <ModIcon
            className={cn(
              'h-[18px] w-[18px] transition-colors duration-200',
              isModuleActive ? 'text-primary' : 'text-muted-foreground',
            )}
          />
        </Link>
      </PopoverPrimitive.Trigger>
      <PopoverPrimitive.Portal>
        <PopoverPrimitive.Content
          side="right"
          align="start"
          sideOffset={8}
          onMouseEnter={cancelClose}
          onMouseLeave={scheduleClose}
          onOpenAutoFocus={(event) => {
            event.preventDefault();
          }}
          className="z-50 min-w-[200px] rounded-lg border p-2 shadow-lg"
          style={{
            background: 'hsl(var(--color-popover))',
            color: 'hsl(var(--color-popover-foreground))',
            borderColor: 'hsl(var(--color-border))',
          }}
        >
          <p className="text-muted-foreground/70 px-3 pb-1 text-[0.7rem] font-semibold tracking-widest uppercase">
            {t(mod.translationKey)}
          </p>
          <div className="space-y-0.5">
            {mod.navItems.map((item) => (
              <SubNavLink
                key={item.href}
                item={item}
                modBasePath={mod.basePath}
                location={location}
                t={t}
                onClose={() => {
                  onClose?.();
                  setOpen(false);
                }}
              />
            ))}
          </div>
        </PopoverPrimitive.Content>
      </PopoverPrimitive.Portal>
    </PopoverPrimitive.Root>
  );
}

interface SubNavLinkProps {
  item: ReturnType<typeof getModules>[number]['navItems'][number];
  modBasePath: string;
  location: Location;
  t: TFunction;
  onClose?: () => void;
}

function SubNavLink({ item, modBasePath, location, t, onClose }: SubNavLinkProps) {
  const isActive =
    item.href === modBasePath
      ? location.pathname === item.href
      : location.pathname === item.href || location.pathname.startsWith(item.href);
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
      <span className="flex-1">{t(item.translationKey)}</span>
      {item.Badge ? <item.Badge /> : null}
    </Link>
  );
}
