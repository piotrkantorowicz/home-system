import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { GlassRow } from './GlassRow';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

describe('GlassRow', () => {
  it('renders one filled glass per full glassMl of totalMl', () => {
    render(
      <GlassRow
        totalMl={500}
        targetMl={2500}
        glassMl={250}
        onAdd={vi.fn()}
        onRemoveNewest={vi.fn()}
      />,
    );

    expect(screen.getAllByRole('button', { name: 'hydration.remove_glass_aria' })).toHaveLength(2);
  });

  it('shows the actual millilitres on a partial glass, non-interactively', () => {
    render(
      <GlassRow
        totalMl={300}
        targetMl={2500}
        glassMl={250}
        onAdd={vi.fn()}
        onRemoveNewest={vi.fn()}
      />,
    );

    expect(screen.getByText('50')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /50/ })).not.toBeInTheDocument();
  });

  it('calls onRemoveNewest when a filled glass is clicked', async () => {
    const onRemoveNewest = vi.fn();
    render(
      <GlassRow
        totalMl={250}
        targetMl={2500}
        glassMl={250}
        onAdd={vi.fn()}
        onRemoveNewest={onRemoveNewest}
      />,
    );

    await userEvent.click(screen.getByRole('button', { name: 'hydration.remove_glass_aria' }));
    expect(onRemoveNewest).toHaveBeenCalledOnce();
  });

  it('calls onAdd when the first empty glass is clicked, and renders no other clickable empty glasses', async () => {
    const onAdd = vi.fn();
    render(
      <GlassRow totalMl={0} targetMl={1000} glassMl={250} onAdd={onAdd} onRemoveNewest={vi.fn()} />,
    );

    const addButton = screen.getByRole('button', { name: 'hydration.add_glass_aria' });

    await userEvent.click(addButton);
    expect(onAdd).toHaveBeenCalledOnce();
  });

  it('disables interaction when addDisabled/removeDisabled are set', () => {
    render(
      <GlassRow
        totalMl={250}
        targetMl={1000}
        glassMl={250}
        onAdd={vi.fn()}
        onRemoveNewest={vi.fn()}
        addDisabled
        removeDisabled
      />,
    );

    expect(screen.getByRole('button', { name: 'hydration.remove_glass_aria' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'hydration.add_glass_aria' })).toBeDisabled();
  });
});
