import { render, screen } from '@testing-library/react';

import { WeightChart } from './WeightChart';

import type { WeightEntryDto } from '@modules/diet-planner/api/hooks/useWeightEntries';
import type * as Recharts from 'recharts';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (_key: string, fallback: string) => fallback,
    i18n: { language: 'en' },
  }),
}));

// Recharts uses ResponsiveContainer that needs a size; mock it for jsdom.
vi.mock('recharts', async () => {
  const actual = await vi.importActual<typeof Recharts>('recharts');
  return {
    ...actual,
    ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
      <div data-testid="responsive-container" style={{ width: 400, height: 300 }}>
        {children}
      </div>
    ),
  };
});

describe('WeightChart', () => {
  it('renders empty state when no entries', () => {
    render(<WeightChart entries={[]} />);
    expect(screen.getByTestId('weight-chart-empty')).toBeInTheDocument();
  });

  it('renders chart container when entries exist', () => {
    const entries: WeightEntryDto[] = [
      { id: '1', date: '2024-01-01', weightKg: 80, createdAt: '2024-01-01T00:00:00Z' },
      { id: '2', date: '2024-01-08', weightKg: 79, createdAt: '2024-01-08T00:00:00Z' },
    ];
    render(<WeightChart entries={entries} />);
    expect(screen.getByTestId('weight-chart')).toBeInTheDocument();
    expect(screen.queryByTestId('weight-chart-empty')).not.toBeInTheDocument();
  });

  it('renders chart with single entry without trend line', () => {
    const entries: WeightEntryDto[] = [
      { id: '1', date: '2024-01-01', weightKg: 80, createdAt: '2024-01-01T00:00:00Z' },
    ];
    render(<WeightChart entries={entries} />);
    expect(screen.getByTestId('weight-chart')).toBeInTheDocument();
  });
});
