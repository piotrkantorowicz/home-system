import { ToastProvider } from '@shared/context/ToastContext';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';

import RecipeCreate from './RecipeCreate';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));
vi.mock('../../components/recipes/RecipeForm', () => ({
  RecipeForm: ({
    onSubmit,
  }: {
    onSubmit: (data: {
      name: string;
      servings: number;
      visibility: string;
      ingredients: [];
    }) => Promise<void>;
  }) => (
    <button
      onClick={() => {
        void onSubmit({ name: 'Oats', servings: 1, visibility: 'Private', ingredients: [] });
      }}
    >
      Save recipe
    </button>
  ),
}));

it('opens the saved recipe detail after creation so a photo can be added', async () => {
  server.use(
    http.post('http://localhost:5050/api/v1/recipes', () =>
      HttpResponse.json('saved-id', { status: 201 }),
    ),
  );
  const Wrapper = createWrapper();
  render(
    <Wrapper>
      <ToastProvider>
        <MemoryRouter initialEntries={['/recipes/new']}>
          <Routes>
            <Route path="/recipes/new" element={<RecipeCreate />} />
            <Route path="/diet-planner/recipes/saved-id" element={<p>Saved recipe detail</p>} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </Wrapper>,
  );
  await userEvent.click(screen.getByRole('button', { name: 'Save recipe' }));
  expect(await screen.findByText('Saved recipe detail')).toBeInTheDocument();
});
