import { mergeTests } from '@playwright/test';

import { test as authTest } from './auth.fixture';
import { test as dataTest } from './data.fixture';

export const test = mergeTests(authTest, dataTest);
export { expect } from '@playwright/test';
