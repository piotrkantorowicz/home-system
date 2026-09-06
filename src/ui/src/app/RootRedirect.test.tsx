import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, beforeEach, vi } from 'vitest';

import RootRedirect from './RootRedirect';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

let modules: { name: string; basePath: string }[] = [];

vi.mock('@shared/lib/module-registry', async (orig) => {
  // eslint-disable-next-line @typescript-eslint/consistent-type-imports
  const actual = await orig<typeof import('@shared/lib/module-registry')>();
  return { ...actual, getModules: () => modules };
});

function renderAt() {
  return render(
    <MemoryRouter initialEntries={['/']}>
      <Routes>
        <Route path="/" element={<RootRedirect />} />
        <Route path="/diet-planner" element={<div>Diet planner home</div>} />
        <Route path="/notifications" element={<div>Notifications home</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  window.localStorage.clear();
});

describe('RootRedirect', () => {
  it('redirects straight to the only registered module', () => {
    modules = [{ name: 'diet-planner', basePath: '/diet-planner' }];
    renderAt();
    expect(screen.getByText('Diet planner home')).toBeInTheDocument();
  });

  it('redirects to the last-visited module when several are registered', () => {
    modules = [
      { name: 'diet-planner', basePath: '/diet-planner' },
      { name: 'notifications', basePath: '/notifications' },
    ];
    window.localStorage.setItem('home-system-last-module', 'notifications');
    renderAt();
    expect(screen.getByText('Notifications home')).toBeInTheDocument();
  });

  it('falls back to the first module when nothing was persisted', () => {
    modules = [
      { name: 'diet-planner', basePath: '/diet-planner' },
      { name: 'notifications', basePath: '/notifications' },
    ];
    renderAt();
    expect(screen.getByText('Diet planner home')).toBeInTheDocument();
  });

  it('shows an onboarding empty state when no module is installed', () => {
    modules = [];
    renderAt();
    expect(screen.getByText('common.no_modules_title')).toBeInTheDocument();
  });
});
