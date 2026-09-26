import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { ProductForm } from './ProductForm';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
  }),
}));

async function fillRequired() {
  await userEvent.type(screen.getByLabelText('product_form.name_label'), 'Oats');
  for (const label of [
    'product_form.calories_label',
    'product_form.protein_label',
    'product_form.carbs_label',
    'product_form.fat_label',
  ]) {
    await userEvent.type(screen.getByLabelText(label), '1');
  }
}

describe('ProductForm visibility', () => {
  it('defaults a new product to Household and submits it', async () => {
    const onSubmit = vi.fn();
    render(<ProductForm onSubmit={onSubmit} submitLabel="save" />);

    expect(screen.getByLabelText('visibility.label')).toHaveValue('Household');
    await fillRequired();
    await userEvent.click(screen.getByRole('button', { name: 'save' }));

    expect(onSubmit).toHaveBeenCalledOnce();
    expect(onSubmit.mock.calls[0]?.[0]).toMatchObject({ name: 'Oats', visibility: 'Household' });
  });

  it('submits the chosen visibility', async () => {
    const onSubmit = vi.fn();
    render(<ProductForm onSubmit={onSubmit} submitLabel="save" />);

    await fillRequired();
    await userEvent.selectOptions(screen.getByLabelText('visibility.label'), 'Private');
    await userEvent.click(screen.getByRole('button', { name: 'save' }));

    expect(onSubmit.mock.calls[0]?.[0]).toMatchObject({ visibility: 'Private' });
  });

  it('hides the selector when the caller may not change visibility', () => {
    render(<ProductForm onSubmit={vi.fn()} canChangeVisibility={false} />);
    expect(screen.queryByLabelText('visibility.label')).not.toBeInTheDocument();
  });
});
