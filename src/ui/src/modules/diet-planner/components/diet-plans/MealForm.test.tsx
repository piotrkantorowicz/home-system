import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { MemoryRouter } from 'react-router-dom';

import { createWrapper } from '../../../../test/utils/queryWrapper';

import { MealForm } from './MealForm';

import type { MealAssignee } from './MealForm';

import { server } from '@/test/mocks/server';

const BASE = 'http://localhost:5050';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en', changeLanguage: vi.fn() },
  }),
}));

vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue(null),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const family: MealAssignee[] = [
  { value: '', name: 'Me' },
  { value: 'kid-1', name: 'Kid' },
];

function renderForm(props: { assignees?: MealAssignee[]; mode?: 'create' | 'edit' }) {
  const Wrapper = createWrapper();
  render(
    <Wrapper>
      <MemoryRouter>
        <MealForm open onClose={vi.fn()} onSubmit={vi.fn()} mode="create" {...props} />
      </MemoryRouter>
    </Wrapper>,
  );
}

describe('MealForm — assign to', () => {
  it('loads the slots of the person the meal is assigned to', async () => {
    const schedulePersons: (string | null)[] = [];
    server.use(
      http.get(`${BASE}/api/v1/meal-schedule`, ({ request }) => {
        schedulePersons.push(new URL(request.url).searchParams.get('personId'));
        return HttpResponse.json({ id: 's', slots: [] });
      }),
    );
    renderForm({ assignees: family });

    const picker = screen.getByRole('combobox', { name: 'meal_form.person_label' });
    expect(picker).toHaveValue('');
    await userEvent.selectOptions(picker, 'kid-1');

    await waitFor(() => {
      expect(schedulePersons).toEqual([null, 'kid-1']);
    });
  });

  it('shows no picker when the caller can only plan for themselves', () => {
    renderForm({ assignees: [{ value: '', name: 'Me' }] });

    expect(
      screen.queryByRole('combobox', { name: 'meal_form.person_label' }),
    ).not.toBeInTheDocument();
  });

  it('shows no picker when editing', () => {
    renderForm({ assignees: family, mode: 'edit' });

    expect(
      screen.queryByRole('combobox', { name: 'meal_form.person_label' }),
    ).not.toBeInTheDocument();
  });
});
