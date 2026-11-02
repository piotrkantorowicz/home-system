import { initI18n } from '@shared/lib/i18n';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { investmentsModule } from '../index';

import { Gain } from './Gain';

beforeEach(() => {
  initI18n([investmentsModule]);
});

describe('Gain', () => {
  it('signs a gain and adds its percent of what was invested', () => {
    render(<Gain amount="50.00" currency="PLN" invested="200.00" />);

    expect(screen.getByText(/50.00 PLN/)).toHaveTextContent('+50.00 PLN (+25%)');
    expect(screen.getByText(/50.00 PLN/)).toHaveClass('text-good');
  });

  it('shows zero without a sign, in a neutral colour', () => {
    render(<Gain amount="0.00" currency="PLN" invested="200.00" />);

    expect(screen.getByText(/0.00 PLN/)).toHaveTextContent(/^0.00 PLN \(0%\)$/);
    expect(screen.getByText(/0.00 PLN/)).toHaveClass('text-text-2');
  });
});
