import { test as setup } from '@playwright/test';

import {
  authStatePath,
  credentialsFor,
  inviteeAuthStatePath,
  inviteeCredentials,
} from './auth-paths';
import { loginViaAuthentik } from './authentik-login';
import { ensureHousehold } from './household-seed';

const WORKER_COUNT = 4;

for (let workerIndex = 0; workerIndex < WORKER_COUNT; workerIndex++) {
  setup(`authenticate worker ${workerIndex}`, async ({ browser }) => {
    const { username, password } = credentialsFor(workerIndex);

    // Isolated browser context so each login starts from a clean slate and we
    // can save storage state without interference from sibling setup tests.
    const context = await browser.newContext();
    const page = await context.newPage();

    await loginViaAuthentik(page, username, password);

    await context.storageState({ path: authStatePath(workerIndex) });
    await context.close();

    // The SPA gates every module route behind household membership — seed one
    // per worker so specs land on the page they navigate to.
    await ensureHousehold(workerIndex);
  });
}

setup('authenticate invitee', async ({ browser }) => {
  const { username, password } = inviteeCredentials();

  const context = await browser.newContext();
  const page = await context.newPage();

  await loginViaAuthentik(page, username, password);

  await context.storageState({ path: inviteeAuthStatePath() });
  await context.close();

  // Deliberately no ensureHousehold() — this identity stays outside the WORKER_COUNT
  // pool specifically so cross-user specs (e.g. household invitations) have a real
  // second Authentik user with no household of its own to accept into.
});
