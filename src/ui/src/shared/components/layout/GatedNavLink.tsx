import { useNavigationAccess } from '@shared/context/NavigationAccessContext';
import { cn } from '@shared/lib/utils';
import { NavLink } from 'react-router-dom';

import type { ReactNode } from 'react';

export interface GatedNavLinkProps {
  href: string;
  end: boolean;
  className: (active: boolean) => string;
  /** Called after a navigation click, e.g. to close the sheet the link sits in. */
  onNavigate?: () => void;
  children: ReactNode;
}

/**
 * A nav link that honours the navigation restriction (household not set up yet): destinations
 * other than the allowed one render as a disabled, explained placeholder instead of a link.
 */
export function GatedNavLink({ href, end, className, onNavigate, children }: GatedNavLinkProps) {
  const access = useNavigationAccess();

  if (!access.canNavigate(href)) {
    return (
      <span
        aria-disabled="true"
        title={access.reason}
        className={cn(className(false), 'cursor-not-allowed opacity-40')}
      >
        {children}
      </span>
    );
  }

  return (
    <NavLink
      to={href}
      end={end}
      onClick={onNavigate}
      className={({ isActive }) => className(isActive)}
    >
      {children}
    </NavLink>
  );
}
