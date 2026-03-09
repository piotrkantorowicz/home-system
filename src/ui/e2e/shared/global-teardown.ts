import { cleanupTestData } from '../diet-planner/utils/cleanup';

async function globalTeardown() {
  console.log('Running global teardown...');
  await cleanupTestData();
  console.log('Global teardown complete.');
}

export default globalTeardown;
