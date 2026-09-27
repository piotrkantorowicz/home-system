import { useUserRoles } from '@shared/auth/useUserRoles';
import { Badge } from '@shared/components/ui';
import { useQuery } from '@tanstack/react-query';

import { deliveryBacklogOptions } from '../api/hooks/useDeliveryDeadLetters';
import { outboxBacklogOptions } from '../api/hooks/useOutboxDeadLetters';
import { ADMIN_ROLE } from '../constants';

/** Nav counter: dead-lettered deliveries plus dead-lettered events across modules. */
export function DeadLetterBadge() {
  const isAdmin = useUserRoles().includes(ADMIN_ROLE);
  const deliveries = useQuery({ ...deliveryBacklogOptions(), enabled: isAdmin });
  const outbox = useQuery({ ...outboxBacklogOptions(), enabled: isAdmin });

  const total =
    (deliveries.data?.deadLettered ?? 0) +
    (outbox.data ?? []).reduce((sum, m) => sum + m.deadLettered, 0);
  if (total <= 0) return null;

  return (
    <Badge
      variant="destructive"
      aria-label={`${String(total)} dead-lettered`}
      className="h-5 min-w-[1.25rem] justify-center px-1.5 py-0 text-[0.65rem]"
    >
      {total > 99 ? '99+' : String(total)}
    </Badge>
  );
}
