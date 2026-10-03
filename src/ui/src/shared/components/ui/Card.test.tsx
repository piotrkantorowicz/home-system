import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { Card } from './Card';

describe('Card', () => {
  it('is a flat 16px surface without a shadow', () => {
    render(<Card data-testid="card" />);

    const card = screen.getByTestId('card');
    expect(card).toHaveClass('bg-card', 'rounded-lg', 'border');
    expect(card.className).not.toMatch(/shadow/);
  });
});
