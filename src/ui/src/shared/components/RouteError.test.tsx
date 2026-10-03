import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { RouteError } from './RouteError';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

function Boom(): never {
  throw new Error('Bad API response');
}

function renderAt(path: string) {
  const router = createMemoryRouter(
    [
      { path: '/', element: <div>Start page</div> },
      { path: '/callback', element: <Boom />, errorElement: <RouteError /> },
      { path: '/diet-planner/week', element: <Boom />, errorElement: <RouteError /> },
      { path: '*', element: <RouteError /> },
    ],
    { initialEntries: [path] },
  );
  render(<RouterProvider router={router} />);
  return router;
}

describe('RouteError', () => {
  beforeEach(() => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('replaces the router developer page with a friendly message', () => {
    renderAt('/diet-planner/week');

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.getByText('common.route_error.title')).toBeInTheDocument();
    expect(screen.queryByText(/Hey developer/)).not.toBeInTheDocument();
  });

  it('offers a safe way back to the start page', async () => {
    const router = renderAt('/diet-planner/week');

    await userEvent.click(screen.getByRole('link', { name: 'common.route_error.home' }));

    expect(router.state.location.pathname).toBe('/');
    expect(screen.getByText('Start page')).toBeInTheDocument();
  });

  it('retry reloads the failed route', async () => {
    const router = renderAt('/diet-planner/week');
    const navigate = vi.spyOn(router, 'navigate');

    await userEvent.click(screen.getByRole('button', { name: 'common.route_error.retry' }));

    expect(navigate).toHaveBeenCalledWith(0);
  });

  it('restarts from the start page when the auth callback fails', async () => {
    const router = renderAt('/callback');

    await userEvent.click(screen.getByRole('button', { name: 'common.route_error.retry' }));

    expect(router.state.location.pathname).toBe('/');
  });

  it('shows a not-found message without a retry button for unknown routes', () => {
    renderAt('/nope/nothing');

    expect(screen.getByText('common.route_error.not_found_title')).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: 'common.route_error.retry' }),
    ).not.toBeInTheDocument();
  });
});
