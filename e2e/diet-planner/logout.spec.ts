import { test, expect } from './fixtures';
import { LogoutPage } from './pages/logout.page';

test('logout revokes tokens and ends the app session', async ({ page, browser }) => {
  await page.goto('/');
  await expect(page.getByRole('button', { name: /user menu/i })).toBeVisible();

  // Copy the refreshed session without the fixture's init script, which would
  // otherwise reinsert the old tokens on every navigation after logout.
  const context = await browser.newContext({
    storageState: await page.context().storageState(),
  });
  try {
    const logoutTab = await context.newPage();
    const logoutPage = new LogoutPage(logoutTab);
    await logoutPage.goto();

    const revocations = Promise.all(
      ['access_token', 'refresh_token'].map((type) =>
        logoutTab.waitForResponse(
          (response) =>
            new URL(response.url()).pathname === '/authentik/application/o/revoke/' &&
            new URLSearchParams(response.request().postData() ?? '').get('token_type_hint') ===
              type,
        ),
      ),
    );
    await logoutPage.logout();
    const responses = await revocations;
    for (const response of responses) {
      expect(response.status()).toBe(200);
    }

    await logoutPage.expectLoggedOut();
    const refreshRequest = new URLSearchParams(responses[1].request().postData() ?? '');
    const reuse = await context.request.post('http://localhost:9000/application/o/token/', {
      form: {
        grant_type: 'refresh_token',
        client_id: refreshRequest.get('client_id') ?? '',
        refresh_token: refreshRequest.get('token') ?? '',
      },
    });
    expect(reuse.status()).toBe(400);
    expect(await reuse.json()).toMatchObject({ error: 'invalid_grant' });
    const state = await context.storageState();
    const app = state.origins.find((origin) => origin.origin === 'http://localhost:5173');
    expect(app?.localStorage.filter((entry) => entry.name.startsWith('oidc.user:')) ?? []).toEqual(
      [],
    );
  } finally {
    await context.close();
  }
});
