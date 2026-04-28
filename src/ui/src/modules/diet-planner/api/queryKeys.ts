// src/ui/src/modules/diet-planner/api/queryKeys.ts

export const queryKeys = {
  meals: {
    all: () => ['meals'] as const,
    list: (range: { from?: string; to?: string }) => ['meals', range] as const,
  },
  nutritionSummary: {
    all: () => ['nutrition-summary'] as const,
    detail: (params: { from: string; to: string }) => ['nutrition-summary', params] as const,
  },
  goals: {
    detail: () => ['goals'] as const,
  },
  hydration: {
    config: () => ['hydration-config'] as const,
    intake: (date: string) => ['water-intake', date] as const,
  },
  mealSchedule: {
    detail: () => ['meal-schedule'] as const,
  },
  products: {
    all: () => ['products'] as const,
    list: (params: { search?: string; onlyMine?: boolean; page?: number; pageSize?: number }) =>
      ['products', params] as const,
    detail: (id: string) => ['products', id] as const,
  },
  recipes: {
    all: () => ['recipes'] as const,
    list: (params: { search?: string; onlyMine?: boolean; page?: number; pageSize?: number }) =>
      ['recipes', params] as const,
    detail: (id: string) => ['recipes', id] as const,
  },
  profile: {
    detail: () => ['profile'] as const,
  },
  dietReminderSettings: {
    detail: () => ['diet-reminder-settings'] as const,
  },
  weightPrediction: {
    detail: (dailyCalorieTarget: number | null | undefined) =>
      ['weight-prediction', dailyCalorieTarget] as const,
  },
  weightEntries: {
    all: () => ['weight-entries'] as const,
    list: (range: { from?: string; to?: string }) => ['weight-entries', range] as const,
  },
} as const;
