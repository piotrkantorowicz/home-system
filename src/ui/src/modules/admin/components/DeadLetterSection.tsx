import { Badge, Card } from '@shared/components/ui';

import type { ReactNode } from 'react';

interface DeadLetterSectionProps {
  title: string;
  /** Dead letters waiting in this source; zero hides the badge. */
  count: number;
  /** The section's "Retry all". */
  action?: ReactNode;
  children: ReactNode;
}

/** One surface per source of failed messages: title, count badge and Retry all in the header. */
export function DeadLetterSection({ title, count, action, children }: DeadLetterSectionProps) {
  return (
    <Card className="mb-4 overflow-hidden">
      <div className="flex flex-wrap items-center justify-between gap-3 px-4 py-4 md:px-6">
        <div className="flex items-center gap-2.5">
          <h2 className="text-lg font-semibold">{title}</h2>
          {count > 0 ? (
            <Badge variant="destructive" className="tnum">
              {count}
            </Badge>
          ) : null}
        </div>
        {action}
      </div>
      {children}
    </Card>
  );
}
