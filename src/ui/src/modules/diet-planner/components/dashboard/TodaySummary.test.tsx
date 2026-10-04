import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

import { TodaySummary } from './TodaySummary';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
    i18n: { language: 'en' },
  }),
}));
vi.mock('./WaterSummary', () => ({ WaterSummary: () => <div data-testid="water" /> }));

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

const nutrition = (calories: number, protein = 90) => ({
  date: '2026-09-03',
  calories,
  protein,
  carbs: 150,
  fat: 40,
  fiber: 18,
});

const renderSummary = (calories: number | null, protein?: number) =>
  render(
    <TodaySummary
      goals={goals}
      nutrition={calories === null ? undefined : nutrition(calories, protein)}
      date="2026-09-03"
      onSetGoals={vi.fn()}
    />,
  );

describe('TodaySummary', () => {
  it('prompts to set goals when no calorie target exists, and still shows water', () => {
    render(
      <TodaySummary goals={null} nutrition={undefined} date="2026-09-03" onSetGoals={vi.fn()} />,
    );
    expect(screen.getByText('dashboard.goals_cta_title')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'dashboard.goals_cta_button' })).toBeInTheDocument();
    expect(screen.getByTestId('water')).toBeInTheDocument();
  });

  it('shows kcal left and a within-budget pill when under target', () => {
    renderSummary(1240);
    expect(screen.getByText('760')).toBeInTheDocument();
    expect(screen.getByText('dashboard.hero_within_budget')).toBeInTheDocument();
    expect(screen.getByText(/dashboard.hero_left_unit/)).toBeInTheDocument();
  });

  it('stays within budget inside the 3% tolerance band', () => {
    renderSummary(2060);
    expect(screen.getByText('dashboard.hero_within_budget')).toBeInTheDocument();
  });

  it('flips to an over-budget pill and the overshoot amount when above the band', () => {
    renderSummary(2300);
    expect(screen.getByText('300')).toBeInTheDocument();
    expect(screen.getByText('dashboard.hero_over_budget')).toBeInTheDocument();
    expect(screen.getByText(/dashboard.hero_over_unit/)).toBeInTheDocument();
  });

  it('shows an empty day as the full target left with no status pill', () => {
    renderSummary(null);
    expect(screen.getByText('2 000')).toBeInTheDocument();
    expect(screen.getByText('dashboard.nothing_logged')).toBeInTheDocument();
    expect(screen.queryByText('dashboard.hero_within_budget')).not.toBeInTheDocument();
  });

  it('lists each macro against its goal', () => {
    renderSummary(1000, 150);
    expect(screen.getByText('150 / 140 g')).toBeInTheDocument();
    expect(screen.getByText('150 / 240 g')).toBeInTheDocument();
  });
});
