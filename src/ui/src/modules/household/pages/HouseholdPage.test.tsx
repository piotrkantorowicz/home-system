import { ToastProvider } from '@shared/context/ToastContext';
import { initI18n } from '@shared/lib/i18n';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { delay, http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { HouseholdProvider } from '../components/HouseholdProvider';
import { HouseholdRequired } from '../components/HouseholdRequired';
import { householdModule } from '../index';

import HouseholdPage from './HouseholdPage';

import type { Household } from '../types';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

vi.mock('react-oidc-context', () => ({
  useAuth: () => ({ isAuthenticated: true, user: { profile: { sub: 'owner' } } }),
}));
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue('token'),
  tryRenewToken: vi.fn(),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050';
const fixture: Household = {
  id: 'home-1',
  name: 'Our home',
  myRole: 'Owner',
  members: [
    {
      personId: 'owner',
      displayName: 'Alex',
      role: 'Owner',
      isManaged: false,
      nickname: null,
      avatarUrl: null,
    },
    {
      personId: 'child',
      displayName: 'Sam',
      role: 'Child',
      isManaged: true,
      nickname: null,
      avatarUrl: null,
    },
  ],
};
let household: Household | null;

function renderPage(path = '/household') {
  const Wrapper = createWrapper();
  return render(
    <Wrapper>
      <ToastProvider>
        <MemoryRouter initialEntries={[path]}>
          <HouseholdProvider>
            <HouseholdRequired>
              <Routes>
                <Route path="/household" element={<HouseholdPage />} />
                <Route path="/diet-planner" element={<p>Diet planner</p>} />
              </Routes>
            </HouseholdRequired>
          </HouseholdProvider>
        </MemoryRouter>
      </ToastProvider>
    </Wrapper>,
  );
}

beforeEach(() => {
  localStorage.clear();
  initI18n([householdModule]);
  household = structuredClone(fixture);
  server.use(
    http.get(`${BASE}/api/households/me`, () =>
      household ? HttpResponse.json(household) : new HttpResponse(null, { status: 404 }),
    ),
    http.post(`${BASE}/api/persons/me/sync`, () => HttpResponse.json({ personId: 'owner' })),
    http.get(`${BASE}/api/households/home-1/invitations`, () => HttpResponse.json([])),
    http.get(`${BASE}/api/households/invitations/mine`, () => HttpResponse.json([])),
    http.get(`${BASE}/api/households/pickable-persons`, () =>
      HttpResponse.json([
        { personId: 'new-person', displayName: 'Jo', email: 'jo@example.com', isManaged: false },
      ]),
    ),
  );
});

describe('Household UI', () => {
  it('suggests a name and creates a home only after explicit submission', async () => {
    household = null;
    const create = vi.fn();
    server.use(
      http.post(`${BASE}/api/households`, async ({ request }) => {
        create(await request.json());
        household = structuredClone(fixture);
        return new HttpResponse(null, { status: 201 });
      }),
    );
    renderPage('/diet-planner');
    await screen.findByRole('heading', { name: 'Give your home a name' });
    expect(screen.getByLabelText('Household name')).toHaveValue('My home');
    expect(create).not.toHaveBeenCalled();
    await userEvent.clear(screen.getByLabelText('Household name'));
    await userEvent.type(screen.getByLabelText('Household name'), 'Our home');
    await userEvent.click(screen.getByRole('button', { name: 'Get started' }));
    await screen.findByRole('heading', { name: 'Our home' });
    expect(create).toHaveBeenCalledWith({ name: 'Our home' });
  });

  it('blocks feature routes on lookup failure and allows retry', async () => {
    server.use(
      http.get(`${BASE}/api/households/me`, () => new HttpResponse(null, { status: 500 })),
    );
    renderPage('/diet-planner');
    expect(screen.queryByText('Diet planner')).not.toBeInTheDocument();
    await screen.findByRole('alert');
    server.use(http.get(`${BASE}/api/households/me`, () => HttpResponse.json(fixture)));
    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));
    expect(await screen.findByText('Diet planner')).toBeInTheDocument();
  });

  it('creates the default home without requiring name edits and offers no skip', async () => {
    household = null;
    const create = vi.fn();
    server.use(
      http.post(`${BASE}/api/households`, async ({ request }) => {
        create(await request.json());
        household = { ...fixture, name: 'My home', members: fixture.members.slice(0, 1) };
        return new HttpResponse(null, { status: 201 });
      }),
    );
    renderPage();
    await screen.findByRole('heading', { name: 'Give your home a name' });
    expect(
      screen.queryByRole('link', { name: 'Continue to Diet Planner' }),
    ).not.toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Get started' }));
    await screen.findByRole('heading', { name: 'Your home is ready' });
    expect(create).toHaveBeenCalledWith({ name: 'My home' });
    await userEvent.click(screen.getByRole('link', { name: 'Continue to Diet Planner' }));
    expect(await screen.findByText('Diet planner')).toBeInTheDocument();
  });

  it('makes adding people optional for a single-member household', async () => {
    household = { ...fixture, members: fixture.members.slice(0, 1) };
    renderPage();
    expect(await screen.findByRole('heading', { name: 'Your home is ready' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add someone' })).toBeInTheDocument();
    await userEvent.click(screen.getByRole('link', { name: 'Continue to Diet Planner' }));
    expect(screen.getByText('Diet planner')).toBeInTheDocument();
  });

  it('does not join a household just because sync completed', async () => {
    household = null;
    server.use(
      http.post(`${BASE}/api/persons/me/sync`, async () => {
        await delay(40);
        return HttpResponse.json({ personId: 'person-123' });
      }),
    );
    renderPage('/diet-planner');
    expect(screen.queryByText('Diet planner')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Get started' })).not.toBeInTheDocument();
    expect(
      await screen.findByRole('heading', { name: 'Give your home a name' }),
    ).toBeInTheDocument();
  });

  it('keeps setup visible after a failed creation', async () => {
    household = null;
    server.use(http.post(`${BASE}/api/households`, () => new HttpResponse(null, { status: 500 })));
    renderPage('/diet-planner');
    await userEvent.click(await screen.findByRole('button', { name: 'Get started' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not save this change');
    expect(screen.getByLabelText('Household name')).toHaveValue('My home');
    expect(screen.queryByText('Diet planner')).not.toBeInTheDocument();
  });

  it('does not mistake server errors for onboarding', async () => {
    server.use(
      http.get(`${BASE}/api/households/me`, () => new HttpResponse(null, { status: 500 })),
    );
    renderPage();
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load your household');
    expect(screen.queryByRole('button', { name: 'Get started' })).not.toBeInTheDocument();
  });

  it('protects the last owner while allowing changes to other members', async () => {
    const change = vi.fn();
    server.use(
      http.put(`${BASE}/api/households/home-1/members/child/role`, async ({ request }) => {
        change(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();
    expect(await screen.findByLabelText('Role for Alex')).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Remove Alex' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Leave household' })).toBeDisabled();
    await userEvent.selectOptions(screen.getByLabelText('Role for Sam'), 'Adult');
    await waitFor(() => {
      expect(change).toHaveBeenCalledWith({ role: 'Adult' });
    });
  });

  it.each(['Adult', 'Child', 'Guest'])('hides owner controls from %s members', async (role) => {
    household = { ...fixture, myRole: role };
    renderPage();
    await screen.findByRole('heading', { name: 'Our home' });
    expect(screen.queryByRole('button', { name: 'Add member' })).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Role for Sam')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Delete household' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Leave household' })).toBeEnabled();
  });

  it.each(['existing', 'managed', 'invite'] as const)(
    'submits the %s add-member flow',
    async (mode) => {
      const post = vi.fn();
      const endpoint =
        mode === 'existing' ? 'members' : mode === 'managed' ? 'managed-members' : 'invitations';
      server.use(
        http.post(`${BASE}/api/households/home-1/${endpoint}`, async ({ request }) => {
          post(await request.json());
          return mode === 'managed'
            ? new HttpResponse(null, { status: 204 })
            : HttpResponse.json({ invitationId: 'invite-1' });
        }),
      );
      renderPage();
      await userEvent.click(await screen.findByRole('button', { name: 'Add member' }));
      const dialog = within(screen.getByRole('dialog'));
      if (mode === 'existing') {
        await dialog.findByRole('option', { name: 'Jo (jo@example.com)' });
        await userEvent.selectOptions(dialog.getByLabelText('Person'), 'new-person');
      } else {
        await userEvent.click(
          dialog.getByRole('tab', { name: mode === 'managed' ? 'Managed member' : 'By email' }),
        );
        await userEvent.type(
          dialog.getByLabelText(mode === 'managed' ? 'Display name' : 'Email address'),
          mode === 'managed' ? 'Taylor' : 'jo@example.com',
        );
      }
      await userEvent.click(
        dialog.getByRole('button', { name: mode === 'invite' ? 'Invite by email' : 'Add member' }),
      );
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      });
      expect(post).toHaveBeenCalledWith(
        mode === 'existing'
          ? { personId: 'new-person', role: 'Adult', nickname: null }
          : mode === 'managed'
            ? { displayName: 'Taylor', role: 'Child', email: null, nickname: null }
            : { email: 'jo@example.com', role: 'Adult' },
      );
      if (mode !== 'managed')
        expect(
          screen.getByText('Invitation created. They must accept it before they join.'),
        ).toBeInTheDocument();
    },
  );

  it('keeps failed invitation form open with entered values', async () => {
    server.use(
      http.post(
        `${BASE}/api/households/home-1/invitations`,
        () => new HttpResponse(null, { status: 409 }),
      ),
    );
    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Add member' }));
    const dialog = within(screen.getByRole('dialog'));
    await userEvent.click(dialog.getByRole('tab', { name: 'By email' }));
    await userEvent.type(dialog.getByLabelText('Email address'), 'jo@example.com');
    await userEvent.click(dialog.getByRole('button', { name: 'Invite by email' }));
    await dialog.findByRole('alert');
    expect(dialog.getByLabelText('Email address')).toHaveValue('jo@example.com');
  });

  it('requires confirmation before removing a member', async () => {
    const remove = vi.fn();
    server.use(
      http.delete(`${BASE}/api/households/home-1/members/child`, () => {
        remove();
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();
    await userEvent.click(await screen.findByRole('button', { name: 'Remove Sam' }));
    expect(remove).not.toHaveBeenCalled();
    await userEvent.click(
      within(screen.getByRole('dialog')).getByRole('button', { name: 'Cancel' }),
    );
    expect(remove).not.toHaveBeenCalled();
    await userEvent.click(screen.getByRole('button', { name: 'Remove Sam' }));
    await userEvent.click(
      within(screen.getByRole('dialog')).getByRole('button', { name: 'Confirm' }),
    );
    await waitFor(() => {
      expect(remove).toHaveBeenCalledOnce();
    });
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('accepts a pending invitation and then shows the joined household', async () => {
    household = null;
    server.use(
      http.get(`${BASE}/api/households/invitations/mine`, () =>
        HttpResponse.json([
          {
            id: 'invite-1',
            householdId: 'home-1',
            householdName: 'Our home',
            role: 'Adult',
            createdAt: '2026-09-08T10:00:00Z',
            expiresAt: '2026-10-08T10:00:00Z',
          },
        ]),
      ),
      http.post(`${BASE}/api/households/invitations/invite-1/accept`, () => {
        household = { ...fixture, myRole: 'Adult' };
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();
    await screen.findByText('Our home invited you as Adult');
    await userEvent.click(screen.getByRole('button', { name: 'Accept' }));
    expect(await screen.findByText("You've joined the Our home household.")).toBeInTheDocument();
    await screen.findByRole('heading', { name: 'Our home' });
  });

  it('declines a pending invitation without joining', async () => {
    household = null;
    let pending = true;
    server.use(
      http.get(`${BASE}/api/households/invitations/mine`, () =>
        HttpResponse.json(
          pending
            ? [
                {
                  id: 'invite-1',
                  householdId: 'home-1',
                  householdName: 'Our home',
                  role: 'Adult',
                  createdAt: '2026-09-08T10:00:00Z',
                  expiresAt: '2026-10-08T10:00:00Z',
                },
              ]
            : [],
        ),
      ),
      http.post(`${BASE}/api/households/invitations/invite-1/decline`, () => {
        pending = false;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();
    await screen.findByText('Our home invited you as Adult');
    await userEvent.click(screen.getByRole('button', { name: 'Decline' }));
    expect(await screen.findByText('Invitation declined.')).toBeInTheDocument();
    await screen.findByRole('heading', { name: 'Give your home a name' });
  });

  it('shows a loading state for pending invitations instead of treating it as none', async () => {
    household = null;
    server.use(
      http.get(`${BASE}/api/households/invitations/mine`, async () => {
        await delay('infinite');
        return HttpResponse.json([]);
      }),
    );
    renderPage();
    expect(await screen.findByText('Loading household…')).toBeInTheDocument();
  });

  it('shows a retryable error for pending invitations instead of treating it as none', async () => {
    household = null;
    let fail = true;
    server.use(
      http.get(`${BASE}/api/households/invitations/mine`, () =>
        fail ? new HttpResponse(null, { status: 500 }) : HttpResponse.json([]),
      ),
    );
    renderPage();
    const banner = await screen.findByRole('alert');
    expect(banner).toHaveTextContent('Could not load invitations.');
    expect(screen.getByRole('heading', { name: 'Give your home a name' })).toBeInTheDocument();
    fail = false;
    await userEvent.click(within(banner).getByRole('button', { name: 'Retry' }));
    await waitFor(() => {
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });
  });

  it('blocks feature access when person sync fails', async () => {
    server.use(
      http.post(`${BASE}/api/persons/me/sync`, () => new HttpResponse(null, { status: 500 })),
    );
    renderPage('/diet-planner');
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load your household');
    expect(screen.queryByText('Diet planner')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Get started' })).not.toBeInTheDocument();
  });

  it('renames the household and reports success', async () => {
    server.use(
      http.put(`${BASE}/api/households/home-1`, async ({ request }) => {
        const body = (await request.json()) as { name: string };
        household = { ...fixture, name: body.name };
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();
    const name = await screen.findByLabelText('Household name');
    await userEvent.clear(name);
    await userEvent.type(name, 'New home');
    await userEvent.click(screen.getByRole('button', { name: 'Save name' }));
    await screen.findByRole('heading', { name: 'New home' });
    expect(await screen.findByText('Household name saved.')).toBeInTheDocument();
  });

  it.each(['leave', 'delete'] as const)('returns to setup after confirming %s', async (action) => {
    if (action === 'leave') household = { ...fixture, myRole: 'Adult' };
    const perform = vi.fn(() => {
      household = null;
      return new HttpResponse(null, { status: 204 });
    });
    server.use(
      action === 'leave'
        ? http.post(`${BASE}/api/households/home-1/leave`, perform)
        : http.delete(`${BASE}/api/households/home-1`, perform),
    );
    renderPage();
    await userEvent.click(
      await screen.findByRole('button', {
        name: action === 'leave' ? 'Leave household' : 'Delete household',
      }),
    );
    expect(perform).not.toHaveBeenCalled();
    await userEvent.click(
      within(screen.getByRole('dialog')).getByRole('button', { name: 'Confirm' }),
    );
    await screen.findByRole('heading', { name: 'Give your home a name' });
    expect(perform).toHaveBeenCalledOnce();
  });

  it('revokes a pending invitation after confirmation', async () => {
    let pending = true;
    server.use(
      http.get(`${BASE}/api/households/home-1/invitations`, () =>
        HttpResponse.json(
          pending
            ? [
                {
                  id: 'invite-1',
                  email: 'jo@example.com',
                  role: 'Adult',
                  status: 'Pending',
                  createdAt: '2026-09-08T10:00:00Z',
                  expiresAt: '2026-10-08T10:00:00Z',
                },
              ]
            : [],
        ),
      ),
      http.delete(`${BASE}/api/households/home-1/invitations/invite-1`, () => {
        pending = false;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderPage();
    await userEvent.click(
      await screen.findByRole('button', { name: 'Revoke invitation for jo@example.com' }),
    );
    expect(pending).toBe(true);
    await userEvent.click(
      within(screen.getByRole('dialog')).getByRole('button', { name: 'Confirm' }),
    );
    expect(await screen.findByText('No pending invitations.')).toBeInTheDocument();
  });
});
