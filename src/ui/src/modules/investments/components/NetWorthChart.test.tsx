import { initI18n } from '@shared/lib/i18n';
import { render, screen } from '@testing-library/react';
import { cloneElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { investmentsModule } from '../index';

import { NetWorthChart } from './NetWorthChart';

import type * as Recharts from 'recharts';

// jsdom has no layout, so give the chart a fixed size instead of measuring its container.
vi.mock('recharts', async () => {
  const actual = await vi.importActual<typeof Recharts>('recharts');
  return {
    ...actual,
    ResponsiveContainer: ({
      children,
    }: {
      children: React.ReactElement<{ width?: number; height?: number }>;
    }) => cloneElement(children, { width: 400, height: 200 }),
  };
});

const points = [
  { date: '2026-08-31', total: '1000.00', incomplete: false, invested: null },
  { date: '2026-09-30', total: '1200.00', incomplete: false, invested: '900.00' },
];

beforeEach(() => {
  initI18n([investmentsModule]);
});

describe('NetWorthChart', () => {
  it('draws an invested line next to net worth when there are contributions', () => {
    render(<NetWorthChart points={points} />);

    expect(screen.getByText('Invested')).toBeInTheDocument();
    expect(screen.getByText('Net worth')).toBeInTheDocument();
  });

  it('draws no invested line without contributions', () => {
    render(<NetWorthChart points={points.map((p) => ({ ...p, invested: null }))} />);

    expect(screen.queryByText('Invested')).not.toBeInTheDocument();
  });
});
