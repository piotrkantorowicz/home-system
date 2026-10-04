import { ToastProvider } from '@shared/context/ToastContext';
import { render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { createWrapper } from '../../../test/utils/queryWrapper';

import Hydration from './Hydration';

import { server } from '@/test/mocks/server';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
    i18n: { language: 'en', changeLanguage: vi.fn() },
  }),
}));

const BASE = 'http://localhost:5050';
const config = (over: Record<string, unknown> = {}) =>
  http.get(`${BASE}/api/v1/hydration/config`, () =>
    HttpResponse.json({
      id: 'c',
      userId: 'u',
      dailyWaterTargetMl: 2500,
      glassSizeMl: 250,
      trackWaterIntake: true,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: null,
      ...over,
    }),
  );

function renderPage() {
  const Wrapper = createWrapper();
  render(
    <Wrapper>
      <ToastProvider>
        <MemoryRouter>
          <Hydration />
        </MemoryRouter>
      </ToastProvider>
    </Wrapper>,
  );
}

describe('Hydration page', () => {
  it('offers 250/330/500 ml quick adds and lists entries newest first', async () => {
    server.use(
      config(),
      http.get(`${BASE}/api/v1/hydration/intake`, () =>
        HttpResponse.json({
          date: 'x',
          totalMl: 750,
          entries: [
            { id: '1', amountMl: 250, timestamp: '2024-01-15T08:00:00', note: 'first' },
            { id: '2', amountMl: 500, timestamp: '2024-01-15T20:00:00', note: 'last' },
          ],
        }),
      ),
    );
    renderPage();

    await screen.findByText('hydration.quick_glass');
    expect(screen.getByRole('button', { name: /\+330/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /\+500/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'hydration.custom_trigger' })).toBeInTheDocument();

    const notes = screen.getAllByText(/^(first|last)$/).map((n) => n.textContent);
    expect(notes).toEqual(['last', 'first']);
    expect(screen.getByText(/hydration.drinks_count.*"count":2/)).toBeInTheDocument();
  });

  it('states the goal-met denominator over completed days', async () => {
    server.use(
      config(),
      http.get(`${BASE}/api/v1/hydration/intake`, () =>
        HttpResponse.json({ date: 'x', totalMl: 3000, entries: [] }),
      ),
    );
    renderPage();

    // every day reports 3000 ml against a 2500 ml goal: 6 completed days, all met
    expect(await screen.findByText(/hydration.goal_met.*"met":6,"total":6/)).toBeInTheDocument();
  });

  it('represents disabled tracking without logging controls', async () => {
    server.use(config({ trackWaterIntake: false }));
    renderPage();

    expect(await screen.findByText(/hydration.tracking_off/)).toBeInTheDocument();
    expect(screen.queryByText('hydration.quick_glass')).not.toBeInTheDocument();
  });

  it('shows a deep link to water settings and no progress bar without a goal', async () => {
    server.use(config({ dailyWaterTargetMl: 0 }));
    renderPage();

    await waitFor(() => {
      expect(screen.getByText(/hydration.no_goal/)).toBeInTheDocument();
    });
    expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: 'hydration.water_settings' })[0]).toHaveAttribute(
      'href',
      '/diet-planner/profile?section=hydration',
    );
  });
});
