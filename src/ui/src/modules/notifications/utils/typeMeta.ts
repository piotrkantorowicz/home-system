import { AlertCircle, BarChart3, Bell, Droplet, Trophy, Utensils } from 'lucide-react';

import type { LucideIcon } from 'lucide-react';

export interface TypeMeta {
  icon: LucideIcon;
  labelKey: string; // i18n key under the 'notifications' namespace
}

const REGISTRY: Record<string, TypeMeta> = {
  MealReminder: { icon: Utensils, labelKey: 'types.meal_reminder' },
  MealMissed: { icon: AlertCircle, labelKey: 'types.meal_missed' },
  WaterReminder: { icon: Droplet, labelKey: 'types.water_reminder' },
  WeeklySummary: { icon: BarChart3, labelKey: 'types.weekly_summary' },
  GoalMilestone: { icon: Trophy, labelKey: 'types.goal_milestone' },
};

const FALLBACK: TypeMeta = { icon: Bell, labelKey: 'types.unknown' };

export function getTypeMeta(type: string): TypeMeta {
  return REGISTRY[type] ?? FALLBACK;
}
