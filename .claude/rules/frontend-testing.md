# Frontend — Testing (Vitest + Testing Library + MSW)

## Stack

| Layer | Tool |
|---|---|
| Unit / component / hook | Vitest 4 (`globals: true`, jsdom) + `@testing-library/react` 16 |
| User events | `@testing-library/user-event` |
| DOM matchers | `@testing-library/jest-dom/vitest` (imported in `src/test/setup.ts`) |
| API mocking | MSW 2 — `src/test/mocks/{handlers,server}.ts` |
| E2E | Playwright — see `frontend-playwright.md` |

Config: `src/ui/vitest.config.ts`. `testTimeout: 15000` (lazy page imports), `e2e/**` excluded,
aliases mirror `vite.config.ts`. Coverage is reported (v8) but not thresholded — new code is
expected to arrive with tests per `definition-of-done.md`, not to chase a percentage.

## Setup (`src/test/setup.ts`)

```ts
import '@testing-library/jest-dom/vitest';
import { server } from './mocks/server';

server.listen({ onUnhandledRequest: 'warn' });   // top level: patches fetch before any client module captures it
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
```

MSW handlers match the **absolute** API URL (`http://localhost:5050/api/v1/…`) because the
`openapi-fetch` clients are created with a base URL. A test that needs a different response
calls `server.use(http.get(...))` inline — it is reset after each test.

## Where tests live

Co-located: `Component.test.tsx` next to `Component.tsx`, `useGoals.test.ts` next to
`useGoals.ts`. No `__tests__` folders. Shared fixtures in `src/test/`.

## Component tests

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { productFactory } from '@/test/factories/product';

import { ProductCard } from './ProductCard';

describe('ProductCard', () => {
  it('shows the product name', () => {
    render(<ProductCard product={productFactory({ name: 'Oats' })} />);
    expect(screen.getByRole('heading', { name: 'Oats' })).toBeInTheDocument();
  });

  it('calls onSelect with the id when the select button is clicked', async () => {
    const onSelect = vi.fn();
    render(<ProductCard product={productFactory({ id: 'p1' })} onSelect={onSelect} />);

    await userEvent.click(screen.getByRole('button', { name: /select/i }));

    expect(onSelect).toHaveBeenCalledExactlyOnceWith('p1');
  });
});
```

- Components that use `useTranslation` render under the real i18n instance (initialised in
  setup) — assert on English text, or on roles/labels, not on keys.
- Components that use queries render inside `createWrapper()` from `src/test/utils/queryWrapper.tsx`
  (fresh `QueryClient`, `retry: false`).
- Components that use router hooks render inside `createMemoryRouter` / `RouterProvider`.

### Query priority

1. `getByRole` (with `name`) — resilient, checks accessibility for free
2. `getByLabelText` — form fields
3. `getByPlaceholderText`, `getByText`, `getByDisplayValue`
4. `getByTestId` — last resort; add `data-testid` only for elements with no accessible identity
   (the e2e suite shares the same ids)

`findBy*` for anything that appears after an await; never `waitFor` around a `getBy` that could
simply be a `findBy`.

## Hook tests

```ts
import { renderHook, waitFor } from '@testing-library/react';

import { createWrapper } from '@/test/utils/queryWrapper';

import { useGoals } from './useGoals';

it('maps the DTO into the view model', async () => {
  const { result } = renderHook(() => useGoals(), { wrapper: createWrapper() });

  await waitFor(() => expect(result.current.isSuccess).toBe(true));

  expect(result.current.data?.calories).toBe(2200);
});
```

## Factories

`src/test/factories/<entity>.ts` — plain functions with overrides, deterministic ids
(`String(++counter)`), no faker. Keep the wire shape (the generated DTO), not the view model,
unless the factory is explicitly for a mapped type.

## What to test

| Worth it | Skip |
|---|---|
| User interactions and visible outcomes | Tailwind classes, exact DOM structure |
| Mapping / formatting logic in hooks and utils | TanStack Query internals |
| Error, empty and loading states | Third-party primitives (Radix) |
| a11y attributes on custom widgets | `console.*` calls |
| i18n: both languages produce a label | Snapshot tests of whole pages |

## Rules

- No `test.skip` / `it.only` in committed code (`forbidOnly` is on in CI for Playwright; for
  Vitest the reviewer rejects it).
- No fixed `setTimeout` waits; use `findBy*` / `waitFor` with a real condition, or
  `vi.useFakeTimers()` + `vi.advanceTimersByTime()` for time-based logic.
- Mock at the network boundary (MSW), not by `vi.mock`-ing your own hooks — except for
  infrastructure singletons (`userManager`, `ToastContext`) where a light stub is fine.
- One behaviour per `it`. Test names read as sentences.
