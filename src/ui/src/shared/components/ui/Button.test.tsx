import { render, screen } from '@testing-library/react';
import { createRef } from 'react';
import { describe, expect, it } from 'vitest';

import { Button } from './Button';

describe('Button', () => {
  it('exposes its native button through ref', () => {
    const ref = createRef<HTMLButtonElement>();

    render(<Button ref={ref}>Save</Button>);

    expect(ref.current).toBe(screen.getByRole('button', { name: 'Save' }));
  });

  it('composes its ref onto the child when using asChild', () => {
    const ref = createRef<HTMLButtonElement>();
    const childRef = createRef<HTMLButtonElement>();

    render(
      <Button asChild ref={ref}>
        <button type="button" ref={childRef}>
          Settings
        </button>
      </Button>,
    );

    const button = screen.getByRole('button', { name: 'Settings' });
    expect(ref.current).toBe(button);
    expect(childRef.current).toBe(button);
  });
});
