import sharedEn from '@shared/locales/en.json';
import sharedPl from '@shared/locales/pl.json';
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createInstance } from 'i18next';
import { http, HttpResponse } from 'msw';
import { createContext, useContext } from 'react';
import { I18nextProvider } from 'react-i18next';
import { MemoryRouter } from 'react-router-dom';

import { recipeImageOptions, useChangeRecipeImage } from '../../api/hooks/useRecipeImage';
import { useRecipeImageScope } from '../../hooks/useRecipeImageScope';
import en from '../../locales/en.json';
import pl from '../../locales/pl.json';
import RecipeList from '../../pages/recipes/RecipeList';

import { RecipeCard } from './RecipeCard';

import type { RecipeDto } from '../../api/hooks/useRecipes';

import { server } from '@/test/mocks/server';
import { HouseholdWrapper } from '@/test/utils/householdWrapper';

const auth = { isAuthenticated: true, user: { profile: { sub: 'user-1' } } };
const AuthTestContext = createContext(auth);
vi.mock('react-oidc-context', () => ({ useAuth: () => useContext(AuthTestContext) }));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: vi.fn(), error: vi.fn() }),
}));
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue('token'),
  tryRenewToken: vi.fn().mockResolvedValue(null),
  redirectToLogin: vi.fn(),
}));

const intersections = new Map<Element, (near: boolean) => void>();
class TestIntersectionObserver implements IntersectionObserver {
  readonly root = null;
  readonly rootMargin = '200px';
  readonly thresholds = [0];
  private targets = new Set<Element>();
  private callback: IntersectionObserverCallback;
  constructor(callback: IntersectionObserverCallback) {
    this.callback = callback;
  }
  observe(target: Element) {
    this.targets.add(target);
    intersections.set(target, (near) => {
      this.callback(
        [
          {
            target,
            isIntersecting: near,
            intersectionRatio: near ? 1 : 0,
            boundingClientRect: new DOMRect(),
            intersectionRect: new DOMRect(),
            rootBounds: null,
            time: 0,
          },
        ],
        this,
      );
    });
  }
  unobserve(target: Element) {
    intersections.delete(target);
    this.targets.delete(target);
  }
  disconnect() {
    for (const target of this.targets) this.unobserve(target);
  }
  takeRecords() {
    return [];
  }
}

const image = {
  version: 'v1',
  width: 100,
  height: 50,
  size: 123,
  thumbnailWidth: 100,
  thumbnailHeight: 50,
  thumbnailSize: 50,
  contentType: 'image/jpeg',
};
let recipe: RecipeDto;
let client: QueryClient;
let language: ReturnType<typeof createInstance>;
const readThumbnail = vi.fn();
const readList = vi.fn();
const readDetail = vi.fn();
const createUrl = vi.fn();
const revokeUrl = vi.fn();
const onDelete = vi.fn();

beforeEach(async () => {
  vi.clearAllMocks();
  intersections.clear();
  auth.isAuthenticated = true;
  auth.user.profile.sub = 'user-1';
  recipe = {
    id: 'r1',
    name: 'Protein bowl',
    description: null,
    instructions: null,
    servings: 2,
    prepTimeMinutes: 10,
    createdByUserId: 'user-1',
    createdAt: '2026-10-11T00:00:00Z',
    updatedAt: null,
    visibility: 'Public',
    isOwner: true,
    canEdit: true,
    ingredients: [],
    image,
    nutritionPerServing: { calories: 500, protein: 45, carbs: 30, fat: 12, fiber: 5 },
  };
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  language = createInstance();
  await language.init({
    lng: 'en',
    fallbackLng: 'en',
    resources: {
      en: { translation: { ...en, ...sharedEn } },
      pl: { translation: { ...pl, ...sharedPl } },
    },
  });
  let nextUrl = 0;
  createUrl.mockImplementation(() => `blob:thumbnail-${String(++nextUrl)}`);
  vi.stubGlobal('IntersectionObserver', TestIntersectionObserver);
  vi.spyOn(URL, 'createObjectURL').mockImplementation(createUrl);
  vi.spyOn(URL, 'revokeObjectURL').mockImplementation(revokeUrl);
  server.use(
    http.get('http://localhost:5050/api/v1/recipes', () => {
      readList();
      return HttpResponse.json({
        items: [recipe],
        totalCount: 1,
        page: 1,
        pageSize: 50,
        totalPages: 1,
      });
    }),
    http.get('http://localhost:5050/api/v1/recipes/r1', () => HttpResponse.json(recipe)),
    http.get('http://localhost:5050/api/v1/recipes/r1/image/thumbnail', ({ request }) => {
      readThumbnail(request.headers.get('Authorization'), request.url);
      return new HttpResponse(new Uint8Array([1, 2]), {
        headers: { 'Content-Type': 'image/jpeg' },
      });
    }),
    http.get('http://localhost:5050/api/v1/recipes/r1/image', () => {
      readDetail();
      return new HttpResponse(new Uint8Array([1, 2, 3, 4, 5, 6]), {
        headers: { 'Content-Type': 'image/jpeg' },
      });
    }),
    http.delete('http://localhost:5050/api/v1/recipes/r1/image', () => {
      recipe = { ...recipe, image: null };
      return new HttpResponse(null, { status: 204 });
    }),
  );
});
afterEach(() => {
  client.clear();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

function DetailRead() {
  const scope = useRecipeImageScope();
  const query = useQuery({
    ...recipeImageOptions(recipe.id, image.version, scope ?? ''),
    enabled: !!scope,
  });
  return <p>Detail bytes: {query.data?.size ?? 0}</p>;
}
function EditableList() {
  const scope = useRecipeImageScope();
  const change = useChangeRecipeImage(recipe.id, scope ?? '');
  return (
    <>
      <RecipeList />
      <button
        onClick={() => {
          change.mutate(null);
        }}
      >
        Remove image
      </button>
    </>
  );
}

function renderCard(
  options: { role?: string; loading?: boolean; list?: boolean; detail?: boolean } = {},
) {
  const content = (role = options.role ?? 'Owner', loading = options.loading ?? false) => (
    <AuthTestContext
      value={{
        isAuthenticated: auth.isAuthenticated,
        user: { profile: { sub: auth.user.profile.sub } },
      }}
    >
      <I18nextProvider i18n={language}>
        <QueryClientProvider client={client}>
          <HouseholdWrapper myRole={role} isLoading={loading}>
            <MemoryRouter>
              {options.list ? <EditableList /> : <RecipeCard recipe={recipe} onDelete={onDelete} />}
              {options.detail ? <DetailRead /> : null}
            </MemoryRouter>
          </HouseholdWrapper>
        </QueryClientProvider>
      </I18nextProvider>
    </AuthTestContext>
  );
  const view = render(content());
  return {
    ...view,
    refresh: (role?: string, loading = false) => {
      view.rerender(content(role, loading));
    },
  };
}
const card = () => screen.getByRole('listitem', { name: recipe.name });
// Decorative thumbnails intentionally have no accessible name or keyboard stop.
const thumbnail = () => card().querySelector('img');
function setNear(near: boolean) {
  act(() => {
    intersections.get(card())?.(near);
  });
}
async function expectThumbnail() {
  await waitFor(() => expect(thumbnail()).toBeInTheDocument());
}

it('loads only the authenticated thumbnail when the card enters the viewport margin', async () => {
  renderCard();
  expect(thumbnail()).toBeNull();
  expect(readThumbnail).not.toHaveBeenCalled();
  setNear(true);
  await expectThumbnail();
  expect(readThumbnail).toHaveBeenCalledExactlyOnceWith(
    'Bearer token',
    'http://localhost:5050/api/v1/recipes/r1/image/thumbnail',
  );
  expect(readDetail).not.toHaveBeenCalled();
  expect(thumbnail()).toHaveAttribute('alt', '');
  expect(thumbnail()).toHaveAttribute('aria-hidden', 'true');
  expect(thumbnail()).toHaveAttribute('width', '48');
  expect(thumbnail()).toHaveAttribute('height', '48');
  expect(within(card()).getAllByRole('img')).toHaveLength(1); // Only the nutrition chart is announced.
  expect(screen.getByRole('link', { name: recipe.name })).toHaveAttribute(
    'href',
    '/diet-planner/recipes/r1',
  );
  expect(screen.getByText('Public')).toBeInTheDocument();
  expect(screen.getByText(/500/)).toBeInTheDocument();
});

it('keeps cards without photos free of image elements and image requests', () => {
  recipe = { ...recipe, image: null };
  renderCard();
  setNear(true);
  expect(thumbnail()).toBeNull();
  expect(readThumbnail).not.toHaveBeenCalled();
  expect(readDetail).not.toHaveBeenCalled();
  expect(screen.getByRole('link', { name: recipe.name })).toBeInTheDocument();
});

it.each([403, 404, 500])(
  'falls back after HTTP %s while retaining card content and actions',
  async (status) => {
    server.use(
      http.get('http://localhost:5050/api/v1/recipes/r1/image/thumbnail', () => {
        readThumbnail();
        return new HttpResponse(null, { status });
      }),
    );
    renderCard();
    setNear(true);
    await waitFor(() => {
      expect(readThumbnail).toHaveBeenCalledOnce();
    });
    await waitFor(() => {
      expect(client.isFetching()).toBe(0);
    });
    expect(thumbnail()).toBeNull();
    expect(screen.getByText('Public')).toBeInTheDocument();
    expect(screen.getByText(/500/)).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Actions' }));
    expect(screen.getByRole('menuitem', { name: 'Edit' })).toBeInTheDocument();
    await userEvent.click(screen.getByRole('menuitem', { name: 'Delete' }));
    expect(onDelete).toHaveBeenCalledOnce();
    expect(readThumbnail).toHaveBeenCalledOnce();
  },
);

it('drops a broken image and releases its URL without a retry loop', async () => {
  renderCard();
  setNear(true);
  await expectThumbnail();
  const element = thumbnail();
  if (!element) throw new Error('Thumbnail is missing');
  fireEvent.error(element);
  expect(thumbnail()).toBeNull();
  expect(revokeUrl).toHaveBeenCalledWith('blob:thumbnail-1');
  expect(readThumbnail).toHaveBeenCalledOnce();
});

it('releases thumbnail bytes and URLs when a card leaves the viewport or unmounts', async () => {
  const view = renderCard();
  setNear(true);
  await expectThumbnail();
  setNear(false);
  expect(thumbnail()).toBeNull();
  expect(revokeUrl).toHaveBeenCalledWith('blob:thumbnail-1');
  await waitFor(() => {
    expect(client.getQueriesData({ queryKey: ['recipe-images'] })).toHaveLength(0);
  });
  setNear(true);
  await expectThumbnail();
  expect(readThumbnail).toHaveBeenCalledTimes(2);
  view.unmount();
  expect(revokeUrl).toHaveBeenCalledWith('blob:thumbnail-2');
  expect(intersections.size).toBe(0);
});

it('clears images on identity changes and logout', async () => {
  const view = renderCard();
  setNear(true);
  await expectThumbnail();
  server.use(
    http.get('http://localhost:5050/api/v1/recipes/r1/image/thumbnail', () => {
      readThumbnail();
      return new HttpResponse(null, { status: 404 });
    }),
  );
  auth.user.profile.sub = 'user-2';
  view.refresh();
  expect(thumbnail()).toBeNull();
  await waitFor(() => {
    expect(readThumbnail).toHaveBeenCalledTimes(2);
  });
  expect(revokeUrl).toHaveBeenCalledWith('blob:thumbnail-1');
  auth.isAuthenticated = false;
  view.refresh();
  expect(thumbnail()).toBeNull();
});

it('waits for household scope and refreshes images when roles change', async () => {
  const view = renderCard({ loading: true });
  setNear(true);
  expect(readThumbnail).not.toHaveBeenCalled();
  view.refresh();
  await expectThumbnail();
  view.refresh('Guest');
  await expectThumbnail();
  expect(readThumbnail).toHaveBeenCalledTimes(2);
  expect(revokeUrl).toHaveBeenCalledWith('blob:thumbnail-1');
});

it('uses distinct query entries for detail and thumbnail variants', async () => {
  renderCard({ detail: true });
  setNear(true);
  await expectThumbnail();
  await screen.findByText('Detail bytes: 6');
  expect(readDetail).toHaveBeenCalledOnce();
  expect(readThumbnail).toHaveBeenCalledOnce();
  expect(createUrl.mock.calls[0]?.[0]).toHaveProperty('size', 2);
});

it('replaces versioned thumbnails and removes them when metadata is cleared', async () => {
  const view = renderCard();
  setNear(true);
  await expectThumbnail();
  recipe = { ...recipe, image: { ...image, version: 'v2' } };
  view.refresh();
  await waitFor(() => expect(thumbnail()).toHaveAttribute('src', 'blob:thumbnail-2'));
  expect(revokeUrl).toHaveBeenCalledWith('blob:thumbnail-1');
  recipe = { ...recipe, image: null };
  view.refresh();
  expect(thumbnail()).toBeNull();
  expect(revokeUrl).toHaveBeenCalledWith('blob:thumbnail-2');
  expect(readThumbnail).toHaveBeenCalledTimes(2);
});

it('updates real list mapping and removes thumbnails after a successful image mutation', async () => {
  renderCard({ list: true });
  await screen.findByRole('listitem', { name: recipe.name });
  setNear(true);
  await expectThumbnail();
  await userEvent.click(screen.getByRole('button', { name: 'Remove image' }));
  await waitFor(() => {
    expect(readList).toHaveBeenCalledTimes(2);
  });
  await waitFor(() => {
    expect(client.isFetching()).toBe(0);
  });
  expect(thumbnail()).toBeNull();
  expect(revokeUrl).toHaveBeenCalledWith('blob:thumbnail-1');
  expect(screen.getByRole('link', { name: recipe.name })).toBeInTheDocument();
});

it('retains Polish labels and hides editor actions from readers', async () => {
  recipe = { ...recipe, canEdit: false };
  await language.changeLanguage('pl');
  renderCard({ role: 'Guest' });
  setNear(true);
  await expectThumbnail();
  expect(screen.getByRole('link', { name: recipe.name })).toBeInTheDocument();
  await userEvent.click(screen.getByRole('button', { name: sharedPl.common.actions }));
  expect(screen.getByRole('menuitem', { name: sharedPl.common.view })).toBeInTheDocument();
  expect(screen.queryByRole('menuitem', { name: sharedPl.common.edit })).not.toBeInTheDocument();
});
