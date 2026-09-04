import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';

import { RecipeForm } from './RecipeForm';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts ? `${key} ${JSON.stringify(opts)}` : key,
  }),
}));

vi.mock('@modules/diet-planner/api/hooks/useProducts', () => ({
  useProducts: () => ({ data: { items: [] } }),
}));

describe('RecipeForm', () => {
  it('renders the basic-info fields and the ingredients section', () => {
    render(<RecipeForm onSubmit={vi.fn()} />);

    expect(screen.getByLabelText('recipe_form.name_label')).toBeInTheDocument();
    expect(screen.getByLabelText('recipe_form.servings_label')).toBeInTheDocument();
    expect(screen.getByText('recipe_form.ingredients_header')).toBeInTheDocument();
  });

  it('blocks submit and shows validation errors when required fields are empty', async () => {
    const onSubmit = vi.fn();
    render(<RecipeForm onSubmit={onSubmit} submitLabel="recipe_form.create_btn" />);

    await userEvent.click(screen.getByRole('button', { name: 'recipe_form.create_btn' }));

    expect(await screen.findByText('Recipe name is required')).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('adds an ingredient row when "add ingredient" is clicked', async () => {
    render(<RecipeForm onSubmit={vi.fn()} />);

    const before = screen.getAllByLabelText('recipe_form.product_label').length;
    await userEvent.click(screen.getByRole('button', { name: /recipe_form.add_ingredient/ }));
    const after = screen.getAllByLabelText('recipe_form.product_label').length;

    expect(after).toBe(before + 1);
  });
});
