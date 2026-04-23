import { cleanupWorker } from '../diet-planner/utils/cleanup';

import type { FullConfig } from '@playwright/test';

async function globalTeardown(config: FullConfig) {
  console.log('Running global teardown...');
  console.log('Purging test data via test-support endpoint for each worker...');

  // Run cleanups in parallel — each worker owns a different Authentik user,
  // so there's no contention on the backend.
  const workerCount = config.workers;
  await Promise.all(
    Array.from({ length: workerCount }, (_, i) => cleanupWorker(i)),
  );

  console.log('Global teardown complete.');
}

export default globalTeardown;
