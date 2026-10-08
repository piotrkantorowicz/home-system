import { queryKeys } from '@modules/diet-planner/api/queryKeys';
import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { beforeEach, describe, expect, it } from 'vitest';

import { dietPlannerModule } from '../../index';

import { GoalsForm } from './GoalsForm';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

const GOALS_URL = 'http://localhost:5050/api/v1/goals';

let stored: { dailyCalorieTarget: number | null; proteinGrams: number | null };

const goalsBody = () => ({
  id: '55555555-5555-5555-5555-555555555555',
  userId: 'user-1',
  dailyCalorieTarget: stored.dailyCalorieTarget,
  proteinGrams: stored.proteinGrams,
  carbsGrams: null,
  fatGrams: null,
  fiberGrams: null,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: null,
});

beforeEach(() => {
  initI18n([dietPlannerModule]);
  stored = { dailyCalorieTarget: 2000, proteinGrams: 100 };
  server.use(
    http.get(GOALS_URL, () => HttpResponse.json(goalsBody())),
    http.put(GOALS_URL, async ({ request }) => {
      stored = (await request.json()) as typeof stored;
      return HttpResponse.json(goalsBody());
    }),
  );
});

async function renderLoadedForm() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const Wrapper = createWrapper(client);
  render(
    <Wrapper>
      <ToastProvider>
        <GoalsForm />
      </ToastProvider>
    </Wrapper>,
  );
  const input = await screen.findByDisplayValue('2000');
  const save = screen.getByRole('button', { name: /save goals/i });
  return { client, input, save };
}

describe('GoalsForm', () => {
  it('keeps an edit savable when the goals query refetches with new server data', async () => {
    const { client, input, save } = await renderLoadedForm();

    await userEvent.clear(input);
    await userEvent.type(input, '1500');

    stored = { dailyCalorieTarget: 2200, proteinGrams: 120 };
    await client.invalidateQueries({ queryKey: queryKeys.goals.detail() });

    // The untouched field picking up the refetch proves the form re-synced.
    await screen.findByDisplayValue('120');
    expect(input).toHaveValue(1500);
    expect(save).toBeEnabled();
  });

  it('is clean again once a save lands and the goals query refetches', async () => {
    const { input, save } = await renderLoadedForm();

    await userEvent.clear(input);
    await userEvent.type(input, '1500');
    await userEvent.click(save);

    await waitFor(() => {
      expect(stored.dailyCalorieTarget).toBe(1500);
    });
    await waitFor(() => {
      expect(save).toBeDisabled();
    });
    expect(input).toHaveValue(1500);
  });
});
