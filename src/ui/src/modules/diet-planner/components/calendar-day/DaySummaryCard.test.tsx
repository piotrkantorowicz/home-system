import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { DaySummaryCard } from './DaySummaryCard';

const intake = vi.hoisted(() => vi.fn(() => ({ data: { totalMl: 750 } })));
vi.mock('@modules/diet-planner/api/hooks/useHydration', () => ({
  useHydrationConfig: () => ({ data: null }),
  useWaterIntake: intake,
}));
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (key: string) => key }) }));

describe('DaySummaryCard', () => {
  it('queries water for the selected date instead of today', () => {
    render(<DaySummaryCard date="2024-01-15" meals={[]} goals={null} />);
    expect(intake).toHaveBeenCalledWith('2024-01-15');
    expect(screen.getByText('750')).toBeInTheDocument();
  });
});
