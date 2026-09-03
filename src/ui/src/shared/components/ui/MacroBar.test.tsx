import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';

import { MacroBar } from './MacroBar';

describe('MacroBar', () => {
  it('renders the value / target line with thin-space thousands', () => {
    render(<MacroBar label="Carbs" value={1420} target={2400} macro="carbs" />);
    expect(screen.getByText('1 420 / 2 400 g')).toBeInTheDocument();
  });

  it('clamps the fill width at 100% when over target', () => {
    const { container } = render(<MacroBar label="Fat" value={90} target={70} macro="fat" />);
    const fill = container.querySelector<HTMLElement>('[style*="width"]');
    expect(fill?.style.width).toBe('100%');
    // over-target cap is rendered
    expect(container.querySelector('.bg-fat.absolute')).not.toBeNull();
  });

  it('renders a skeleton instead of the track while loading', () => {
    const { container } = render(
      <MacroBar label="Protein" value={0} target={140} macro="protein" loading />,
    );
    expect(container.querySelector('.animate-pulse')).not.toBeNull();
  });
});
