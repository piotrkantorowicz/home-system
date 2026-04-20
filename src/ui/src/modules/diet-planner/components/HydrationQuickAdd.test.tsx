import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => {
      if (key === 'hydration.glasses_count')
        return `${String(opts?.current)} / ${String(opts?.target)} glasses`;
      return key;
    },
  }),
}));

const mockLogIntake = vi.fn();

vi.mock('@modules/diet-planner/api/hooks/useHydration', () => ({
  useHydrationConfig: () => ({
    data: { dailyWaterTargetMl: 2000, glassSizeMl: 250 },
  }),
  useWaterIntake: () => ({
    data: { totalMl: 750, entries: [] },
  }),
  useLogWaterIntake: () => ({
    mutateAsync: mockLogIntake,
    isPending: false,
  }),
  useDeleteWaterIntake: () => ({
    mutateAsync: vi.fn(),
    isPending: false,
  }),
}));

vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));

import { HydrationQuickAdd } from './HydrationQuickAdd';

describe('HydrationQuickAdd', () => {
  it('renders current glass count and target', () => {
    render(
      <MemoryRouter>
        <HydrationQuickAdd />
      </MemoryRouter>,
    );
    // 750ml / 250ml = 3 glasses, target = 2000/250 = 8
    expect(screen.getByText('3 / 8 glasses')).toBeInTheDocument();
  });

  it('calls logIntake when + button is clicked', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter>
        <HydrationQuickAdd />
      </MemoryRouter>,
    );
    const addButton = screen.getByRole('button', { name: /hydration.add_btn/i });
    await user.click(addButton);
    expect(mockLogIntake).toHaveBeenCalledWith(expect.objectContaining({ amountMl: 250 }));
  });

  it('renders the widget title', () => {
    render(
      <MemoryRouter>
        <HydrationQuickAdd />
      </MemoryRouter>,
    );
    expect(screen.getByText('hydration.quick_title')).toBeInTheDocument();
  });
});
