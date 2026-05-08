import { Badge } from '@shared/components/ui';
import { cn } from '@shared/lib/utils';

import { useUnreadCount } from '../api/hooks/useUnreadCount';

interface UnreadBadgeProps {
  className?: string;
}

export function UnreadBadge({ className }: UnreadBadgeProps) {
  const { data } = useUnreadCount();
  const total = data ? Number(data.total) : 0;
  if (total <= 0) return null;

  const label = total > 9 ? '9+' : String(total);

  return (
    <Badge
      variant="destructive"
      aria-label={`${String(total)} unread`}
      className={cn('h-5 min-w-[1.25rem] justify-center px-1.5 py-0 text-[0.65rem]', className)}
    >
      {label}
    </Badge>
  );
}
