import { cn } from '@shared/lib/utils';
import { type LucideIcon } from 'lucide-react';
import { Link } from 'react-router-dom';

export interface EmptyStateProps {
  icon: LucideIcon;
  title: string;
  description?: string;
  action?:
    | {
        label: string;
        href?: string;
        onClick?: () => void;
      }
    | undefined;
  className?: string;
}

export function EmptyState({ icon: Icon, title, description, action, className }: EmptyStateProps) {
  return (
    <div className={cn('flex flex-col items-center justify-center py-12 text-center', className)}>
      <div className="bg-muted mb-4 rounded-full p-4">
        <Icon className="text-muted-foreground h-8 w-8" />
      </div>
      <h3 className="mb-1 text-lg font-semibold">{title}</h3>
      {description && <p className="text-muted-foreground mb-4 max-w-sm text-sm">{description}</p>}
      {action &&
        (action.href ? (
          <Link
            to={action.href}
            className="bg-primary text-primary-foreground hover:bg-primary/90 inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors"
          >
            {action.label}
          </Link>
        ) : (
          <button
            type="button"
            onClick={action.onClick}
            className="bg-primary text-primary-foreground hover:bg-primary/90 inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors"
          >
            {action.label}
          </button>
        ))}
    </div>
  );
}
