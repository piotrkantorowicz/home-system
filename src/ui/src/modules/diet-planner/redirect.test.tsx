import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, it, expect } from 'vitest';

import { dietPlannerModule } from './index';

describe('legacy diet-planner settings routes', () => {
  it.each(['settings/app', 'preferences'])(
    'redirects /diet-planner/%s to /settings/app',
    (path) => {
      const legacy = dietPlannerModule.routes.find((r) => r.path === path);
      render(
        <MemoryRouter initialEntries={[`/diet-planner/${path}`]}>
          <Routes>
            <Route path={`/diet-planner/${path}`} element={legacy?.element} />
            <Route path="/settings/app" element={<div>app-account</div>} />
          </Routes>
        </MemoryRouter>,
      );
      expect(screen.getByText('app-account')).toBeInTheDocument();
    },
  );
});
