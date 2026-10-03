import { cn } from '@shared/lib/utils';
import { Link } from 'react-router-dom';

import type { ReactNode } from 'react';

export interface PageContainerProps {
  children: ReactNode;
  /** Narrower reading width for forms and lists, kept *inside* the standard container so the title never jumps. */
  width?: 'default' | 'form' | 'narrow';
  className?: string;
}

const widthClass = { default: '', form: 'max-w-[880px]', narrow: 'max-w-[760px]' } as const;

/**
 * The stable page body: 1120px max, left-aligned, 16px padding on phones and 40/48px on desktop.
 * Narrow content passes `width` rather than re-wrapping.
 */
export function PageContainer({ children, width = 'default', className }: PageContainerProps) {
  return (
    <div className="w-full max-w-[1120px] px-4 py-4 md:px-12 md:py-10">
      <div className={cn(widthClass[width], className)}>{children}</div>
    </div>
  );
}

export interface PageHeaderProps {
  /** The page title; equals the nav label. */
  title: string;
  /** One line under the title, e.g. a date or range. */
  subtitle?: string | undefined;
  /** Parent trail; the last crumb is the current page and is not a link. */
  breadcrumb?: readonly { label: string; href?: string }[];
  /** At most one primary (filled) button plus secondary (outlined) ones; wraps on narrow screens. */
  actions?: ReactNode;
  className?: string;
}

export function PageHeader({ title, subtitle, breadcrumb, actions, className }: PageHeaderProps) {
  return (
    <header
      className={cn('mb-6 flex flex-wrap items-start justify-between gap-x-4 gap-y-3', className)}
    >
      <div className="min-w-0">
        {breadcrumb && breadcrumb.length > 0 ? (
          <nav aria-label="Breadcrumb" className="text-muted-foreground text-label mb-1">
            <ol className="flex flex-wrap items-center gap-1">
              {breadcrumb.map((crumb, i) => (
                <li key={`${crumb.label}-${String(i)}`} className="flex items-center gap-1">
                  {i > 0 ? <span aria-hidden="true">›</span> : null}
                  {crumb.href && i < breadcrumb.length - 1 ? (
                    <Link
                      to={crumb.href}
                      className="hover:text-foreground underline-offset-2 hover:underline"
                    >
                      {crumb.label}
                    </Link>
                  ) : (
                    <span aria-current={i === breadcrumb.length - 1 ? 'page' : undefined}>
                      {crumb.label}
                    </span>
                  )}
                </li>
              ))}
            </ol>
          </nav>
        ) : null}
        <h1 className="text-title font-bold tracking-tight">{title}</h1>
        {subtitle ? <p className="text-body text-muted-foreground">{subtitle}</p> : null}
      </div>
      {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
    </header>
  );
}
