/**
 * Single source of truth for per-worker auth-state file locations. Lives in
 * its own module so the test fixture can import it without pulling in the
 * `*.setup.ts` test file (Playwright forbids test files importing each other).
 */
export function authStatePath(workerIndex: number): string {
  return `playwright/.auth/user-${workerIndex}.json`;
}
