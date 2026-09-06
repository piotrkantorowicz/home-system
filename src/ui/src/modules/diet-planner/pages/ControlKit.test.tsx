import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

import ControlKit from './ControlKit';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

describe('ControlKit', () => {
  it('renders every primitive group', () => {
    render(<ControlKit />);

    for (const key of [
      'control_kit.buttons',
      'control_kit.inputs',
      'control_kit.toggles',
      'control_kit.pills',
      'control_kit.data',
      'control_kit.ring',
      'control_kit.banners',
    ]) {
      expect(screen.getByText(key)).toBeInTheDocument();
    }
  });

  it('shows the shared button, switch and banner primitives', () => {
    render(<ControlKit />);

    expect(screen.getByRole('button', { name: 'Primary' })).toBeInTheDocument();
    expect(screen.getByRole('switch', { name: 'demo switch' })).toBeInTheDocument();
    expect(screen.getByText('Could not load.')).toBeInTheDocument();
  });
});
