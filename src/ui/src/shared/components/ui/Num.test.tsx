import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { Num } from './Num';

describe('Num', () => {
  it('renders tabular figures and merges classes', () => {
    render(<Num className="font-bold">2 100</Num>);

    const el = screen.getByText('2 100');
    expect(el).toHaveClass('tabular-nums', 'font-bold');
    expect(el).toHaveAttribute('data-numeric');
  });
});
