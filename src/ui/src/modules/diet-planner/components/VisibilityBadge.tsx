import { Globe, Lock, Users } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { toVisibility } from '../utils/visibility';

const ICON = { Private: Lock, Household: Users, Public: Globe } as const;

export interface VisibilityBadgeProps {
  visibility: string;
}

export function VisibilityBadge({ visibility }: VisibilityBadgeProps) {
  const { t } = useTranslation();
  const value = toVisibility(visibility);
  const Icon = ICON[value];
  return (
    <span className="bg-secondary text-text-2 rounded-6px text-10px inline-flex shrink-0 items-center gap-1 px-1.5 py-0.5 font-bold">
      <Icon className="size-3" aria-hidden />
      {t(`visibility.${value}`)}
    </span>
  );
}
