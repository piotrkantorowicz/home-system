/// <reference types="node" />

import { File as NativeFile } from 'node:buffer';

import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createInstance } from 'i18next';
import { http, HttpResponse } from 'msw';
import { createContext, useContext } from 'react';
import { I18nextProvider } from 'react-i18next';

import { recipeOptions } from '../../api/hooks/useRecipes';
import { queryKeys } from '../../api/queryKeys';
import en from '../../locales/en.json';
import pl from '../../locales/pl.json';

import { RecipePhoto } from './RecipePhoto';

import type { RecipeDto } from '../../api/hooks/useRecipes';

import { server } from '@/test/mocks/server';
import { HouseholdWrapper } from '@/test/utils/householdWrapper';

const auth = { isAuthenticated: true, user: { profile: { sub: 'user-1' } } };
const AuthTestContext = createContext(auth);
const toast = { success: vi.fn(), error: vi.fn() };
const renewal = vi.hoisted(() => vi.fn());
vi.mock('react-oidc-context', () => ({ useAuth: () => useContext(AuthTestContext) }));
vi.mock('@shared/context/ToastContext', () => ({ useToast: () => toast }));
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue('token'),
  tryRenewToken: renewal,
  redirectToLogin: vi.fn(),
}));

const image = {
  version: 'v1',
  width: 100,
  height: 100,
  size: 123,
  thumbnailWidth: 50,
  thumbnailHeight: 50,
  thumbnailSize: 50,
  contentType: 'image/jpeg',
};
let recipe: RecipeDto;
const read = vi.fn();
const upload = vi.fn();
const remove = vi.fn();
const createUrl = vi.fn();
const revokeUrl = vi.fn();
let client: QueryClient;
let language: ReturnType<typeof createInstance>;

beforeEach(async () => {
  recipe = {
    id: 'r1',
    name: 'Oats',
    description: null,
    instructions: null,
    servings: 1,
    prepTimeMinutes: null,
    createdByUserId: 'user-1',
    createdAt: '2026-10-11T00:00:00Z',
    updatedAt: null,
    visibility: 'Private',
    isOwner: true,
    canEdit: true,
    ingredients: [],
    image,
  };
  auth.isAuthenticated = true;
  auth.user.profile.sub = 'user-1';
  vi.clearAllMocks();
  renewal.mockResolvedValue(null);
  // Match fetch's native multipart implementation; jsdom File lacks streaming methods.
  const nativeForm = await new Request('http://localhost', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: '',
  }).formData();
  vi.stubGlobal('FormData', nativeForm.constructor);
  vi.stubGlobal('File', NativeFile);
  let counter = 0;
  createUrl.mockImplementation(() => `blob:photo-${String(++counter)}`);
  vi.stubGlobal(
    'URL',
    Object.assign(URL, { createObjectURL: createUrl, revokeObjectURL: revokeUrl }),
  );
  client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  language = createInstance();
  await language.init({
    lng: 'en',
    fallbackLng: 'en',
    resources: {
      en: { 'diet-planner': en, translation: { common: { cancel: 'Cancel' } } },
      pl: { 'diet-planner': pl, translation: { common: { cancel: 'Anuluj' } } },
    },
  });
  server.use(
    http.get('http://localhost:5050/api/v1/recipes/r1', () => HttpResponse.json(recipe)),
    http.get('http://localhost:5050/api/v1/recipes/r1/image', ({ request }) => {
      read(request.headers.get('Authorization'), request.url);
      return new HttpResponse(new Uint8Array([1, 2, 3]), {
        headers: { 'Content-Type': 'image/jpeg' },
      });
    }),
    http.put('http://localhost:5050/api/v1/recipes/r1/image', async ({ request }) => {
      const form = await request.formData();
      const file = form.get('file');
      upload(file instanceof File ? { name: file.name, type: file.type, size: file.size } : file);
      recipe = { ...recipe, image: { ...image, version: 'v2' } };
      return HttpResponse.json(recipe.image);
    }),
    http.delete('http://localhost:5050/api/v1/recipes/r1/image', () => {
      remove();
      recipe = { ...recipe, image: null };
      return new HttpResponse(null, { status: 204 });
    }),
  );
  client.setQueryData(queryKeys.recipes.detail('r1'), recipe);
});

afterEach(() => {
  client.clear();
  vi.unstubAllGlobals();
});

function PhotoPage() {
  const { data } = useQuery(recipeOptions('r1'));
  return data ? <RecipePhoto recipe={data} /> : <p>Recipe unavailable</p>;
}

function renderPhoto(role = 'Owner') {
  const content = (myRole: string) => (
    <AuthTestContext
      value={{
        isAuthenticated: auth.isAuthenticated,
        user: { profile: { sub: auth.user.profile.sub } },
      }}
    >
      <I18nextProvider i18n={language}>
        <QueryClientProvider client={client}>
          <HouseholdWrapper myRole={myRole}>
            <PhotoPage />
          </HouseholdWrapper>
        </QueryClientProvider>
      </I18nextProvider>
    </AuthTestContext>
  );
  const view = render(content(role));
  return {
    ...view,
    refresh: (myRole = role) => {
      view.rerender(content(myRole));
    },
  };
}

async function selectPhoto() {
  await userEvent.upload(
    screen.getByLabelText('Choose a replacement photo'),
    new File(['new image'], 'photo.png', { type: 'image/png' }),
  );
  await screen.findByRole('img', { name: 'Selected photo preview' });
}

it('reads through bearer-authenticated requests and releases its object URL on unmount', async () => {
  const view = renderPhoto();
  const photo = await screen.findByRole('img', { name: 'Photo of Oats' });
  expect(photo).toHaveAttribute('src', 'blob:photo-1');
  expect(read).toHaveBeenCalledExactlyOnceWith(
    'Bearer token',
    'http://localhost:5050/api/v1/recipes/r1/image',
  );
  view.unmount();
  expect(revokeUrl).toHaveBeenCalledWith('blob:photo-1');
  await waitFor(() => {
    expect(client.getQueriesData({ queryKey: ['recipe-images'] })).toHaveLength(0);
  });
});

it('previews without uploading, then cancel restores the saved photo and file-input focus', async () => {
  renderPhoto();
  const photo = await screen.findByRole('img', { name: 'Photo of Oats' });
  await selectPhoto();
  expect(upload).not.toHaveBeenCalled();
  await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));
  expect(screen.queryByRole('img', { name: 'Selected photo preview' })).not.toBeInTheDocument();
  expect(photo).toHaveAttribute('src', 'blob:photo-1');
  expect(screen.getByLabelText('Choose a replacement photo')).toHaveFocus();
  expect(revokeUrl).toHaveBeenCalledWith('blob:photo-2');
});

it('uploads multipart bytes, updates the photo immediately, and releases the previous image and preview', async () => {
  renderPhoto();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  await selectPhoto();
  await userEvent.click(screen.getByRole('button', { name: 'Upload photo' }));
  await waitFor(() => {
    expect(toast.success).toHaveBeenCalledWith('Photo saved.');
  });
  expect(upload).toHaveBeenCalledExactlyOnceWith({ name: 'photo.png', type: 'image/png', size: 9 });
  expect(screen.getByLabelText('Choose a replacement photo')).toHaveFocus();
  expect(client.getQueryData<RecipeDto>(queryKeys.recipes.detail('r1'))?.image?.version).toBe('v2');
  expect(screen.queryByRole('img', { name: 'Selected photo preview' })).not.toBeInTheDocument();
  expect(revokeUrl).toHaveBeenCalledWith('blob:photo-1');
  expect(revokeUrl).toHaveBeenCalledWith('blob:photo-2');
  expect(await screen.findByRole('img', { name: 'Photo of Oats' })).toHaveAttribute(
    'src',
    'blob:photo-3',
  );
});

it('preserves the old photo and selected file after failure, then retries without creating a recipe', async () => {
  server.use(
    http.put('http://localhost:5050/api/v1/recipes/r1/image', () =>
      HttpResponse.json({}, { status: 500 }),
    ),
  );
  renderPhoto();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  await selectPhoto();
  await userEvent.click(screen.getByRole('button', { name: 'Upload photo' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Photo upload failed');
  expect(screen.getByRole('img', { name: 'Photo of Oats' })).toHaveAttribute('src', 'blob:photo-1');
  expect(screen.getByRole('img', { name: 'Selected photo preview' })).toBeInTheDocument();
  server.use(
    http.put('http://localhost:5050/api/v1/recipes/r1/image', () =>
      HttpResponse.json({ ...image, version: 'v2' }),
    ),
  );
  await userEvent.click(screen.getByRole('button', { name: 'Upload photo' }));
  await waitFor(() => {
    expect(toast.success).toHaveBeenCalledWith('Photo saved.');
  });
});

it('removes the photo without leaving an empty image area', async () => {
  renderPhoto();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  await userEvent.click(screen.getByRole('button', { name: 'Remove photo' }));
  await waitFor(() => {
    expect(toast.success).toHaveBeenCalledWith('Photo removed.');
  });
  expect(remove).toHaveBeenCalledOnce();
  expect(screen.queryByRole('img')).not.toBeInTheDocument();
  expect(screen.getByLabelText('Choose a photo')).toBeInTheDocument();
});

it('shows a reader the photo with no mutation controls', async () => {
  recipe.canEdit = false;
  client.setQueryData(queryKeys.recipes.detail('r1'), recipe);
  renderPhoto('Guest');
  await screen.findByRole('img', { name: 'Photo of Oats' });
  expect(screen.queryByLabelText('Choose a replacement photo')).not.toBeInTheDocument();
  expect(screen.queryByRole('button')).not.toBeInTheDocument();
});

it('renders nothing for a reader of a recipe without a photo', () => {
  recipe.canEdit = false;
  recipe.image = null;
  client.setQueryData(queryKeys.recipes.detail('r1'), recipe);
  renderPhoto('Guest');
  expect(screen.queryByRole('region', { name: 'Recipe photo' })).not.toBeInTheDocument();
  expect(read).not.toHaveBeenCalled();
});

it('rejects unsupported or oversized files before sending requests', async () => {
  renderPhoto();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  const control = screen.getByLabelText('Choose a replacement photo');
  fireEvent.change(control, {
    target: { files: [new File(['svg'], 'photo.svg', { type: 'image/svg+xml' })] },
  });
  expect(screen.getByRole('alert')).toHaveTextContent('Photo could not be accepted');
  fireEvent.change(control, {
    target: {
      files: [new File([new Uint8Array(10 * 1024 * 1024 + 1)], 'large.png', { type: 'image/png' })],
    },
  });
  expect(screen.queryByRole('button', { name: 'Upload photo' })).not.toBeInTheDocument();
  expect(upload).not.toHaveBeenCalled();
});

it('drops cached bytes and preview when identity changes, then clears everything on logout', async () => {
  const view = renderPhoto();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  await selectPhoto();
  server.use(
    http.get(
      'http://localhost:5050/api/v1/recipes/r1/image',
      () => new HttpResponse(null, { status: 404 }),
    ),
  );
  auth.user.profile.sub = 'user-2';
  view.refresh();
  expect(screen.queryByRole('img')).not.toBeInTheDocument();
  await screen.findByText('Photo unavailable. Recipe content remains available.');
  expect(revokeUrl).toHaveBeenCalledWith('blob:photo-1');
  expect(revokeUrl).toHaveBeenCalledWith('blob:photo-2');
  auth.isAuthenticated = false;
  view.refresh();
  expect(screen.queryByRole('region')).not.toBeInTheDocument();
});

it('clears the displayed image on read denial after access changes', async () => {
  renderPhoto();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  server.use(
    http.get(
      'http://localhost:5050/api/v1/recipes/r1/image',
      () => new HttpResponse(null, { status: 403 }),
    ),
  );
  await client.invalidateQueries({ queryKey: ['recipe-images'] });
  await waitFor(() => expect(screen.queryByRole('img')).not.toBeInTheDocument());
  expect(revokeUrl).toHaveBeenCalledWith('blob:photo-1');
});

it('clears preview and displayed image when upload permission is revoked', async () => {
  server.use(
    http.put('http://localhost:5050/api/v1/recipes/r1/image', () =>
      HttpResponse.json({}, { status: 403 }),
    ),
  );
  renderPhoto();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  await selectPhoto();
  await userEvent.click(screen.getByRole('button', { name: 'Upload photo' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Photo access changed');
  expect(screen.queryByRole('img')).not.toBeInTheDocument();
});

it('resets preview and reloads the image after household role changes', async () => {
  const view = renderPhoto();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  await selectPhoto();
  view.refresh('Adult');
  expect(screen.queryByRole('img', { name: 'Selected photo preview' })).not.toBeInTheDocument();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  expect(read).toHaveBeenCalledTimes(2);
});

it('falls back when image rendering fails without retrying requests', async () => {
  renderPhoto();
  fireEvent.error(await screen.findByRole('img', { name: 'Photo of Oats' }));
  expect(
    screen.getByText('Photo unavailable. Recipe content remains available.'),
  ).toBeInTheDocument();
  expect(read).toHaveBeenCalledOnce();
});

it('provides Polish controls and limits', async () => {
  await language.changeLanguage('pl');
  renderPhoto();
  expect(await screen.findByLabelText('Wybierz nowe zdjęcie')).toBeInTheDocument();
  expect(screen.getByText(/Maksymalnie 10 MiB i 25 megapikseli/)).toBeInTheDocument();
});

it('retries a multipart upload after token renewal with the same file bytes', async () => {
  const attempts = vi.fn();
  renewal.mockResolvedValue('renewed-token');
  server.use(
    http.put('http://localhost:5050/api/v1/recipes/r1/image', async ({ request }) => {
      const form = await request.formData();
      const file = form.get('file');
      attempts(
        request.headers.get('Authorization'),
        file instanceof File ? await file.text() : null,
      );
      if (attempts.mock.calls.length === 1) return HttpResponse.json({}, { status: 401 });
      recipe = { ...recipe, image: { ...image, version: 'renewed-version' } };
      return HttpResponse.json(recipe.image);
    }),
  );
  renderPhoto();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  await selectPhoto();
  await userEvent.click(screen.getByRole('button', { name: 'Upload photo' }));
  await waitFor(() => {
    expect(toast.success).toHaveBeenCalledWith('Photo saved.');
  });
  expect(attempts.mock.calls).toEqual([
    ['Bearer token', 'new image'],
    ['Bearer renewed-token', 'new image'],
  ]);
});

it('ignores a late upload response after identity changes', async () => {
  let release: () => void = () => {
    throw new Error('Upload gate is not initialized');
  };
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  const started = vi.fn();
  const finished = vi.fn();
  server.use(
    http.put('http://localhost:5050/api/v1/recipes/r1/image', async () => {
      started();
      await gate;
      finished();
      return HttpResponse.json({ ...image, version: 'late-version' });
    }),
  );
  const view = renderPhoto();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  await selectPhoto();
  await userEvent.click(screen.getByRole('button', { name: 'Upload photo' }));
  await waitFor(() => {
    expect(started).toHaveBeenCalledOnce();
  });
  auth.user.profile.sub = 'user-2';
  view.refresh();
  await screen.findByRole('img', { name: 'Photo of Oats' });
  release();
  await waitFor(() => {
    expect(client.isMutating()).toBe(0);
  });
  expect(finished).toHaveBeenCalledOnce();
  expect(client.getQueryData<RecipeDto>(queryKeys.recipes.detail('r1'))?.image?.version).toBe('v1');
  expect(toast.success).not.toHaveBeenCalled();
});
