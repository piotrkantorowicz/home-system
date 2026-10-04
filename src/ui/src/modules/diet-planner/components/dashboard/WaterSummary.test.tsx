import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import { WaterSummary } from './WaterSummary';

const mocks = vi.hoisted(() => ({
  add: vi.fn(),
  config: {
    data: { dailyWaterTargetMl: 2500, glassSizeMl: 250, trackWaterIntake: true },
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));
vi.mock('@modules/diet-planner/api/hooks/useHydration', () => ({
  useHydrationConfig: () => ({ isPending: false, isError: false, ...mocks.config }),
  useWaterIntake: () => ({
    isPending: false,
    isError: false,
    data: { totalMl: 1330, entries: [] },
  }),
}));
vi.mock('@modules/diet-planner/api/hooks/useWaterActions', () => ({
  useWaterActions: () => ({ add: mocks.add, remove: vi.fn(), pendingKeys: new Set<string>() }),
}));
vi.mock('@modules/diet-planner/components/WaterCustomAmountPopover', () => ({
  WaterCustomAmountPopover: () => <button type="button">custom</button>,
}));

const renderWater = () =>
  render(
    <MemoryRouter>
      <WaterSummary date="2026-10-03" />
    </MemoryRouter>,
  );

describe('WaterSummary', () => {
  it('shows progress toward the target and logs the quick amounts', async () => {
    renderWater();

    expect(screen.getByText('1.3')).toBeInTheDocument();
    expect(screen.getByRole('progressbar', { name: 'dashboard.water_title' })).toHaveAttribute(
      'aria-valuenow',
      '1330',
    );
    await userEvent.click(screen.getByRole('button', { name: '+250 ml' }));
    await userEvent.click(screen.getByRole('button', { name: '+500 ml' }));

    expect(mocks.add).toHaveBeenNthCalledWith(1, 250);
    expect(mocks.add).toHaveBeenNthCalledWith(2, 500);
  });

  it('links to the water settings when tracking is off', () => {
    mocks.config.data = { dailyWaterTargetMl: 2500, glassSizeMl: 250, trackWaterIntake: false };
    renderWater();

    expect(screen.getByRole('link', { name: 'dashboard.water_settings' })).toHaveAttribute(
      'href',
      '/diet-planner/hydration',
    );
    expect(screen.queryByRole('button', { name: '+250 ml' })).not.toBeInTheDocument();
  });
});
