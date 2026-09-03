import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

import { TodayHero } from './TodayHero';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
  }),
}));

const goals = {
  id: 'g1',
  dailyCalorieTarget: 2000,
  proteinGrams: 140,
  carbsGrams: 240,
  fatGrams: 70,
  fiberGrams: 30,
  createdAt: '',
  updatedAt: null,
};

const nutrition = (calories: number) => ({
  date: '2026-09-03',
  calories,
  protein: 90,
  carbs: 150,
  fat: 40,
  fiber: 18,
});

describe('TodayHero', () => {
  it('prompts to set goals when no calorie target exists', () => {
    render(<TodayHero goals={null} nutrition={undefined} onSetGoals={vi.fn()} />);
    expect(screen.getByText('dashboard.goals_cta_title')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'dashboard.goals_cta_button' })).toBeInTheDocument();
  });

  it('shows kcal remaining and a within-budget pill when under target', () => {
    render(<TodayHero goals={goals} nutrition={nutrition(1240)} onSetGoals={vi.fn()} />);
    // 2000 - 1240 = 760 (shown in the ring and the "Remaining" stat)
    expect(screen.getAllByText('760').length).toBeGreaterThan(0);
    expect(screen.getByText('dashboard.hero_within_budget')).toBeInTheDocument();
  });

  it('flips to an over-budget pill and the overshoot amount when above target', () => {
    render(<TodayHero goals={goals} nutrition={nutrition(2300)} onSetGoals={vi.fn()} />);
    expect(screen.getByText('300')).toBeInTheDocument();
    expect(screen.getByText('dashboard.hero_over_budget')).toBeInTheDocument();
  });
});
