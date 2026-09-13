import { render, screen } from '@testing-library/react';
import { createRef } from 'react';
import { describe, expect, it } from 'vitest';

import { Input } from './Input';

describe('Input', () => {
  it('exposes its native input through ref', () => {
    const ref = createRef<HTMLInputElement>();

    render(<Input ref={ref} aria-label="Email" />);

    expect(ref.current).toBe(screen.getByRole('textbox', { name: 'Email' }));
  });
});
