import { render, screen } from '@testing-library/react';

import { WeightProgressCard } from './WeightProgressCard';

import type { WeightEntryDto } from '@modules/diet-planner/api/hooks/useWeightEntries';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (_key: string, fallback: string) => fallback,
    i18n: { language: 'en' },
  }),
}));

const entries: WeightEntryDto[] = [
  { id: '1', date: '2024-01-01', weightKg: 90, createdAt: '2024-01-01T00:00:00Z' },
  { id: '2', date: '2024-02-01', weightKg: 85, createdAt: '2024-02-01T00:00:00Z' },
];

describe('WeightProgressCard', () => {
  it('returns null when target weight is missing', () => {
    const { container } = render(<WeightProgressCard entries={entries} targetWeightKg={null} />);
    expect(container.firstChild).toBeNull();
  });

  it('returns null when no entries', () => {
    const { container } = render(<WeightProgressCard entries={[]} targetWeightKg={80} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders current weight, kg remaining, and progress', () => {
    render(<WeightProgressCard entries={entries} targetWeightKg={80} />);

    expect(screen.getByText('85.0 kg')).toBeInTheDocument();
    expect(screen.getByText('5.0 kg')).toBeInTheDocument();
    expect(screen.getByText(/50%/)).toBeInTheDocument();
    expect(screen.getByTestId('progress-fill')).toHaveStyle({ width: '50%' });
  });
});
