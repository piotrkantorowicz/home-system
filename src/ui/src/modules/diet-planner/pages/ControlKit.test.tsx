import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';

import ControlKit from './ControlKit';

const renderKit = () =>
  render(
    <MemoryRouter>
      <ControlKit />
    </MemoryRouter>,
  );

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

describe('ControlKit', () => {
  it('renders every primitive group', () => {
    renderKit();

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
    renderKit();

    expect(screen.getByRole('button', { name: 'Primary' })).toBeInTheDocument();
    expect(screen.getByRole('switch', { name: 'demo switch' })).toBeInTheDocument();
    expect(screen.getByText('Could not load.')).toBeInTheDocument();
  });

  it('shows preference-aware formatting samples', () => {
    renderKit();

    expect(screen.getByText(/2.100 kcal/)).toBeInTheDocument();
    expect(screen.getByText('1.3 of 2.5 L')).toBeInTheDocument();
  });
});
