import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

import { EnergyModel } from './EnergyModel';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));
vi.mock('@modules/diet-planner/api/hooks/useGoals', () => ({
  useGoals: () => ({ data: { dailyCalorieTarget: 2000 } }),
}));
const prediction = vi.fn<() => unknown>();
vi.mock('@modules/diet-planner/api/hooks/useWeightPrediction', () => ({
  useWeightPrediction: () => ({ data: prediction() }),
}));

describe('EnergyModel', () => {
  it('shows an empty state with an Add details link, never NaN', () => {
    prediction.mockReturnValue(undefined);
    render(<EnergyModel />);
    expect(screen.getByText('profile.energy_empty')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'profile.add_details' })).toHaveAttribute(
      'href',
      '#profile-details',
    );
    expect(screen.queryByText(/NaN/)).not.toBeInTheDocument();
  });

  it('shows the model when a prediction exists', () => {
    prediction.mockReturnValue({
      bmr: 1700,
      tdee: 2400,
      dailyDeficit: 400,
      weeklyWeightChange: -0.4,
      currentBmi: 26.1,
      targetBmi: null,
    });
    render(<EnergyModel />);
    expect(screen.getByText('BMR')).toBeInTheDocument();
    expect(screen.queryByText('profile.energy_empty')).not.toBeInTheDocument();
  });
});
