import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { GoalsForm } from './GoalsForm';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));
const goals = vi.fn<() => unknown>();
const update = vi.fn(() => Promise.resolve());
vi.mock('@modules/diet-planner/api/hooks/useGoals', () => ({
  useGoals: () => ({ data: goals(), isLoading: false }),
  useCreateGoals: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useUpdateGoals: () => ({ mutateAsync: update, isPending: false }),
}));

const serverGoals = (dailyCalorieTarget: number) => ({
  dailyCalorieTarget,
  proteinGrams: null,
  carbsGrams: null,
  fatGrams: null,
  fiberGrams: null,
});

describe('GoalsForm', () => {
  it('keeps an edit savable across a refetch and clean once the save lands', async () => {
    goals.mockReturnValue(serverGoals(2000));
    const { rerender } = render(<GoalsForm />);
    const input = screen.getByLabelText('goals.calories_label');
    const save = screen.getByRole('button', { name: /goals.save_btn/ });
    expect(save).toBeDisabled();

    await userEvent.clear(input);
    await userEvent.type(input, '1500');
    expect(save).toBeEnabled();

    goals.mockReturnValue(serverGoals(2200));
    rerender(<GoalsForm />);
    expect(input).toHaveValue(1500);
    expect(save).toBeEnabled();

    await userEvent.click(save);
    await waitFor(() => {
      expect(update).toHaveBeenCalledOnce();
    });

    // The mutation's invalidation refetches the saved value.
    goals.mockReturnValue(serverGoals(1500));
    rerender(<GoalsForm />);
    await waitFor(() => {
      expect(save).toBeDisabled();
    });
  });
});
