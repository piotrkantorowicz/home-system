import { initI18n } from '@shared/lib/i18n';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { investmentsModule } from '../index';

import InvestmentsPage from './InvestmentsPage';

beforeEach(() => {
  initI18n([investmentsModule]);
});

describe('investmentsModule', () => {
  it('exposes the net worth nav item and an index route under its base path', () => {
    expect(investmentsModule.basePath).toBe('/investments');
    expect(investmentsModule.navItems.map((item) => item.href)).toEqual(['/investments']);
    expect(investmentsModule.routes[0]?.index).toBe(true);
  });
});

describe('InvestmentsPage', () => {
  it('renders the placeholder', () => {
    render(<InvestmentsPage />);

    expect(screen.getByRole('heading', { name: 'Net worth' })).toBeInTheDocument();
    expect(screen.getByText('Coming soon')).toBeInTheDocument();
  });
});
