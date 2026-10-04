import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import { WeekMeters } from './WeekMeters';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
    i18n: { language: 'en' },
  }),
}));

const goals = {
  id: 'g',
  dailyCalorieTarget: 2000,
  proteinGrams: 100,
  carbsGrams: 200,
  fatGrams: 70,
  fiberGrams: 30,
  createdAt: '',
  updatedAt: null,
};

const renderMeters = (totals: Partial<Record<string, number>>, g: typeof goals | null = goals) =>
  render(
    <MemoryRouter>
      <WeekMeters
        totals={{ calories: 0, protein: 0, carbs: 0, fat: 0, fiber: 0, ...totals }}
        goals={g}
      />
    </MemoryRouter>,
  );

describe('WeekMeters', () => {
  it('offers to set goals when there are none', () => {
    renderMeters({}, null);
    expect(screen.getByText('nutrition_summary.goals_cta_title')).toBeInTheDocument();
  });

  it('treats calories, carbs and fat as limits and protein and fibre as minimums', () => {
    renderMeters({ calories: 14000, protein: 800, carbs: 1000, fat: 700, fiber: 250 });

    // calories 14 000 vs 14 000 → on target; protein 800 of 700 → met (over a minimum is fine)
    expect(screen.getAllByText(/calendar.week_status.onTarget/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/calendar.week_status.met/)).toHaveLength(2); // protein and fibre
    // carbs 1 000 of 1 400 → 400 left; fat 700 of 490 → over
    expect(screen.getByText(/calendar.week_status.under/)).toHaveTextContent('400');
    expect(screen.getByText(/calendar.week_status.over/)).toHaveTextContent('210');
  });

  it('says how much of a minimum is still missing', () => {
    renderMeters({ calories: 7000, protein: 400, carbs: 700, fat: 300, fiber: 100 });

    expect(screen.getAllByText(/calendar.week_status.short/)).toHaveLength(2);
  });
});
