/**
 * Shared auth helpers used by both the setup project and the per-test fixture.
 * Lives outside any `*.setup.ts` file so the fixture can import it without
 * pulling in a test file (Playwright forbids test files importing each other).
 */

export function authStatePath(workerIndex: number): string {
  return `playwright/.auth/user-${workerIndex}.json`;
}

/**
 * Returns the Authentik username + password for a given Playwright worker.
 *
 * Precedence:
 *   1. Per-worker override: `TEST_USER_EMAIL_<n>` / `TEST_USER_PASSWORD_<n>`.
 *   2. Shared password: `TEST_USER_PASSWORD` (username still derives from the
 *      worker index as `E2eWorker<n>` to match the Authentik blueprint).
 *
 * Throws when no password is configured — there is no hard-coded fallback so
 * the suite never silently picks up a compromised default.
 */
export function credentialsFor(workerIndex: number): {
  username: string;
  password: string;
} {
  const username = process.env[`TEST_USER_EMAIL_${workerIndex}`] ?? `E2eWorker${workerIndex}`;

  const password =
    process.env[`TEST_USER_PASSWORD_${workerIndex}`] ?? process.env['TEST_USER_PASSWORD'];

  if (!password) {
    throw new Error(
      `TEST_USER_PASSWORD (or TEST_USER_PASSWORD_${workerIndex}) is not set. ` +
        'Copy e2e/.env.example to e2e/.env and set the value — it must match ' +
        'infrastructure/.env → E2E_USER_PASSWORD used by the Authentik blueprint.',
    );
  }

  return { username, password };
}

/**
 * Storage state path for the reserved invitee identity (`E2eInvitee`) — outside the
 * WORKER_COUNT pool and never seeded a household, so it's safe to use as a fresh
 * accept/decline target in cross-user specs without touching a worker's own data.
 */
export function inviteeAuthStatePath(): string {
  return 'playwright/.auth/invitee.json';
}

export function inviteeCredentials(): { username: string; password: string } {
  const username = process.env['TEST_INVITEE_EMAIL'] ?? 'E2eInvitee';
  const password = process.env['TEST_INVITEE_PASSWORD'] ?? process.env['TEST_USER_PASSWORD'];

  if (!password) {
    throw new Error(
      'TEST_INVITEE_PASSWORD (or TEST_USER_PASSWORD) is not set. ' +
        'Copy e2e/.env.example to e2e/.env and set the value — it must match ' +
        'infrastructure/.env → E2E_USER_PASSWORD used by the Authentik blueprint.',
    );
  }

  return { username, password };
}

/**
 * The invitee's real email address — distinct from `inviteeCredentials().username`,
 * which is the Authentik login name. This is what `InvitePersonByEmail` needs to
 * address a `HouseholdInvitation` at them; matches the blueprint's `attrs.email`.
 */
export function inviteeInvitationEmail(): string {
  return process.env['TEST_INVITEE_INVITATION_EMAIL'] ?? 'e2e-invitee@test.local';
}
