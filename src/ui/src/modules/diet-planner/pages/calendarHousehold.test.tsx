/**
 * Calendar as a household view (#419): person filter kept in the URL, meals read for the
 * selected member, and controls hidden where the backend would refuse.
 */
import { ToastProvider } from '@shared/context/ToastContext';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';

import { HouseholdWrapper } from '../../../test/utils/householdWrapper';
import { createWrapper } from '../../../test/utils/queryWrapper';

import Calendar from './Calendar';

import type { HouseholdMember } from '@modules/household';

import { server } from '@/test/mocks/server';

const BASE = 'http://localhost:5050';

const me: HouseholdMember = {
  personId: 'person-1',
  displayName: 'Me',
  avatarUrl: null,
  role: 'Owner',
  nickname: null,
  isManaged: false,
};
const kid: HouseholdMember = {
  personId: 'kid-1',
  displayName: 'Kid',
  avatarUrl: null,
  role: 'Child',
  nickname: null,
  isManaged: true,
};

function todayStr() {
  const d = new Date();
  return `${String(d.getFullYear())}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(
    d.getDate(),
  ).padStart(2, '0')}`;
}

beforeAll(() => {
  Object.defineProperty(window, 'matchMedia', {
    configurable: true,
    value: vi.fn(() => ({
      matches: false,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    })),
  });
});

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

/** Records the personId of every meals request and answers with one meal for that person. */
function captureMealRequests() {
  const personIds: (string | null)[] = [];
  server.use(
    http.get(`${BASE}/api/v1/meals`, ({ request }) => {
      const personId = new URL(request.url).searchParams.get('personId');
      personIds.push(personId);
      return HttpResponse.json([
        {
          id: 'meal-1',
          personId: personId ?? 'person-1',
          personName: personId === 'kid-1' ? 'Kid' : 'Me',
          date: todayStr(),
          mealSlotId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          mealSlotName: 'Breakfast',
          mealSlotSortOrder: 0,
          recipeId: 'recipe-1',
          recipeName: 'Oatmeal',
          servings: 1,
          notes: null,
          mealTime: null,
          sequenceOrder: null,
          status: 'Planned',
          calories: 300,
          protein: 10,
          carbs: 50,
          fat: 5,
          fiber: 4,
          actualRecipe: null,
          actualProducts: [],
        },
      ]);
    }),
  );
  return personIds;
}

function renderCalendar(
  { myRole = 'Owner', members = [me, kid] }: { myRole?: string; members?: HouseholdMember[] },
  url = '/calendar',
) {
  const router = createMemoryRouter([{ path: '/calendar', element: <Calendar /> }], {
    initialEntries: [url],
  });
  const Wrapper = createWrapper();
  render(
    <Wrapper>
      <HouseholdWrapper myRole={myRole} members={members}>
        <ToastProvider>
          <RouterProvider router={router} />
        </ToastProvider>
      </HouseholdWrapper>
    </Wrapper>,
  );
  return router;
}

describe('Calendar — household person filter', () => {
  it('opens on the caller and switches to another member through the URL', async () => {
    const personIds = captureMealRequests();
    const router = renderCalendar({});

    const filter = await screen.findByRole('combobox', { name: 'calendar.person_filter' });
    await waitFor(() => {
      expect(filter).toHaveValue('person-1');
    });
    expect(personIds).not.toContain('kid-1');

    await userEvent.selectOptions(filter, 'kid-1');

    expect(router.state.location.search).toBe('?person=kid-1');
    await waitFor(() => {
      expect(personIds).toContain('kid-1');
    });
    expect(await screen.findByText('Kid', { selector: 'span' })).toBeInTheDocument();
  });

  it('reads the selected member from the URL on load', async () => {
    const personIds = captureMealRequests();
    renderCalendar({}, '/calendar?person=kid-1');

    expect(await screen.findByRole('combobox', { name: 'calendar.person_filter' })).toHaveValue(
      'kid-1',
    );
    await waitFor(() => {
      expect(personIds).toContain('kid-1');
    });
  });

  it('returns to the caller without a diet profile', async () => {
    captureMealRequests();
    server.use(http.get(`${BASE}/api/v1/profile`, () => new HttpResponse(null, { status: 404 })));
    const router = renderCalendar({}, '/calendar?person=kid-1');

    const filter = await screen.findByRole('combobox', { name: 'calendar.person_filter' });
    await userEvent.selectOptions(filter, 'person-1');

    expect(router.state.location.search).toBe('');
  });

  it('shows no filter for a household of one', async () => {
    captureMealRequests();
    renderCalendar({ members: [me] });

    expect(await screen.findAllByRole('button', { name: 'meal_form.add_title' })).not.toHaveLength(
      0,
    );
    expect(
      screen.queryByRole('combobox', { name: 'calendar.person_filter' }),
    ).not.toBeInTheDocument();
  });

  it('hides add and meal actions from a guest', async () => {
    captureMealRequests();
    renderCalendar({ myRole: 'Guest', members: [{ ...me, role: 'Guest' }, kid] });

    expect(await screen.findByText('Oatmeal')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'meal_form.add_title' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Oatmeal/ })).not.toBeInTheDocument();
  });
});
