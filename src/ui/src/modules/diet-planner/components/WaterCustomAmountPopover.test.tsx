import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { WaterCustomAmountPopover } from './WaterCustomAmountPopover';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

describe('WaterCustomAmountPopover', () => {
  it('opens on trigger click and adds the default (330 ml) amount', async () => {
    const onAdd = vi.fn();
    render(<WaterCustomAmountPopover presets={[]} onAdd={onAdd} />);

    await userEvent.click(screen.getByRole('button', { name: 'hydration.custom_trigger' }));
    await userEvent.click(await screen.findByRole('button', { name: /hydration\.add_btn/i }));

    expect(onAdd).toHaveBeenCalledWith(330, undefined);
  });

  it('steps the amount up and down by 50 ml', async () => {
    const onAdd = vi.fn();
    render(<WaterCustomAmountPopover presets={[]} onAdd={onAdd} />);

    await userEvent.click(screen.getByRole('button', { name: 'hydration.custom_trigger' }));
    await userEvent.click(
      await screen.findByRole('button', { name: 'hydration.custom_step_up_aria' }),
    );
    await userEvent.click(screen.getByRole('button', { name: /hydration\.add_btn/i }));

    expect(onAdd).toHaveBeenCalledWith(380, undefined);
  });

  it('selecting a preset overrides the stepper value', async () => {
    const onAdd = vi.fn();
    render(<WaterCustomAmountPopover presets={[200, 1000]} onAdd={onAdd} />);

    await userEvent.click(screen.getByRole('button', { name: 'hydration.custom_trigger' }));
    await userEvent.click(await screen.findByRole('button', { name: '1000' }));
    await userEvent.click(screen.getByRole('button', { name: /hydration\.add_btn/i }));

    expect(onAdd).toHaveBeenCalledWith(1000, undefined);
  });

  it('passes a trimmed note through, or undefined when empty', async () => {
    const onAdd = vi.fn();
    render(<WaterCustomAmountPopover presets={[]} onAdd={onAdd} />);

    await userEvent.click(screen.getByRole('button', { name: 'hydration.custom_trigger' }));
    const noteInput = await screen.findByPlaceholderText('hydration.custom_note_placeholder');
    await userEvent.type(noteInput, '  coffee  ');
    await userEvent.click(screen.getByRole('button', { name: /hydration\.add_btn/i }));

    expect(onAdd).toHaveBeenCalledWith(330, 'coffee');
  });
});
