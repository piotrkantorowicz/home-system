import { defineConfig, mergeConfig } from 'vitest/config';

import viteConfig from './vite.config.ts';

// Reuse the Vite config (React plugin + React Compiler babel plugin + aliases) so the tests
// exercise the same compiled output the build ships.
export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./src/test/setup.ts'],
      // Some suites lazy-import large page modules; keep headroom under parallel load / slow CI.
      testTimeout: 15000,
      exclude: ['**/node_modules/**', '**/e2e/**'],
      coverage: {
        provider: 'v8',
      },
    },
  }),
);
