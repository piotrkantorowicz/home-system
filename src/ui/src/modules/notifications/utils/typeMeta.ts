import { AlertCircle, BarChart3, Bell, Droplet, Trophy, Utensils } from 'lucide-react';

import type { LucideIcon } from 'lucide-react';

export interface TypeMeta {
  icon: LucideIcon;
  labelKey: string; // i18n key under the 'notifications' namespace
  /** Where activating the item leads, with the visible action label. Omitted when there is none. */
  destination?: { href: string; actionKey: string };
}

const TODAY = { href: '/diet-planner', actionKey: 'actions.open_today' };
const NUTRITION = { href: '/diet-planner/nutrition', actionKey: 'actions.see_nutrition' };

const REGISTRY: Record<string, TypeMeta> = {
  MealReminder: { icon: Utensils, labelKey: 'types.meal_reminder', destination: TODAY },
  MealMissed: { icon: AlertCircle, labelKey: 'types.meal_missed', destination: TODAY },
  WaterReminder: {
    icon: Droplet,
    labelKey: 'types.water_reminder',
    destination: { href: '/diet-planner/hydration', actionKey: 'actions.log_water' },
  },
  WeeklySummary: { icon: BarChart3, labelKey: 'types.weekly_summary', destination: NUTRITION },
  GoalMilestone: { icon: Trophy, labelKey: 'types.goal_milestone', destination: NUTRITION },
};

const FALLBACK: TypeMeta = { icon: Bell, labelKey: 'types.unknown' };

export function getTypeMeta(type: string): TypeMeta {
  return REGISTRY[type] ?? FALLBACK;
}
