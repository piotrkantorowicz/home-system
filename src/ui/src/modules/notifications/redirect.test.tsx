import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, it, expect } from 'vitest';

import { notificationsModule } from './index';

describe('legacy channel preferences route', () => {
  it('redirects /notifications/preferences to App & account', () => {
    const legacy = notificationsModule.routes.find((r) => r.path === 'preferences');
    render(
      <MemoryRouter initialEntries={['/notifications/preferences']}>
        <Routes>
          <Route path="/notifications/preferences" element={legacy?.element} />
          <Route path="/settings/app" element={<div>app-account</div>} />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByText('app-account')).toBeInTheDocument();
  });
});
