import '@testing-library/jest-dom/vitest';
import { server } from './mocks/server';

// Start MSW at the top level so it patches globalThis.fetch before any
// module-level code (e.g. openapi-fetch client creation) captures a reference.
server.listen({ onUnhandledRequest: 'warn' });

afterEach(() => {
  server.resetHandlers();
});
afterAll(() => {
  server.close();
});
