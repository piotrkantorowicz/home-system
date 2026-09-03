import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';

import { Ring } from './Ring';

describe('Ring', () => {
  it('places the conic-gradient stop at the given percent', () => {
    const { container } = render(
      <Ring percent={58}>
        <span>910</span>
      </Ring>,
    );
    const ring = container.firstElementChild as HTMLElement;
    expect(ring.style.background).toContain('58%');
    expect(screen.getByText('910')).toBeInTheDocument();
  });

  it('clamps out-of-range percentages', () => {
    const { container } = render(<Ring percent={140}>x</Ring>);
    const ring = container.firstElementChild as HTMLElement;
    expect(ring.style.background).toContain('100%');
    expect(ring.style.background).not.toContain('140%');
  });
});
