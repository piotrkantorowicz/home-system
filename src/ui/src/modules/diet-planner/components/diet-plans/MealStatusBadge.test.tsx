import { render, screen } from '@testing-library/react';

import { MealStatusBadge } from './MealStatusBadge';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

describe('MealStatusBadge', () => {
  it('renders with the correct aria-label for Planned', () => {
    render(<MealStatusBadge status="Planned" />);
    expect(screen.getByLabelText('calendar.meal_status.planned')).toBeInTheDocument();
  });

  it('renders with the correct aria-label for Done', () => {
    render(<MealStatusBadge status="Done" />);
    expect(screen.getByLabelText('calendar.meal_status.done')).toBeInTheDocument();
  });

  it('renders with the correct aria-label for Modified', () => {
    render(<MealStatusBadge status="Modified" />);
    expect(screen.getByLabelText('calendar.meal_status.modified')).toBeInTheDocument();
  });
});
