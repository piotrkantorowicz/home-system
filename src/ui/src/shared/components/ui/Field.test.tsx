import { render, screen } from '@testing-library/react';
import { createRef } from 'react';
import { describe, expect, it } from 'vitest';

import { Field } from './Field';
import { Input } from './Input';

describe('Field', () => {
  it('keeps a nested input ref attached to the labeled control', () => {
    const ref = createRef<HTMLInputElement>();

    render(
      <Field id="email" label="Email address">
        <Input ref={ref} id="email" type="email" />
      </Field>,
    );

    const input = screen.getByLabelText('Email address');
    expect(input).toHaveAttribute('type', 'email');
    expect(ref.current).toBe(input);
  });
});
