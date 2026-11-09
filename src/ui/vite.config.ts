import { readFileSync } from 'node:fs';
import path from 'path';

import babel from '@rolldown/plugin-babel';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

const { version } = JSON.parse(
  readFileSync(path.resolve(import.meta.dirname, 'package.json'), 'utf8'),
) as {
  version: string;
};

// Local OTLP collector for browser tracing, e.g. OTLP_PROXY_TARGET=http://localhost:4318.
const otlpTarget = process.env.OTLP_PROXY_TARGET;

// https://vite.dev/config/
export default defineConfig({
  define: { __APP_VERSION__: JSON.stringify(version) },
  plugins: [
    react(),
    // React Compiler (babel-plugin-react-compiler) — React 19.2+ ships react/compiler-runtime,
    // so no `target` / react-compiler-runtime shim is needed. vitest.config.ts merges this file
    // so tests run against the same compiled output.
    babel({ presets: [reactCompilerPreset()] }),
  ],
  build: {
    rolldownOptions: {
      output: {
        codeSplitting: {
          groups: [
            { name: 'react-vendor', test: /node_modules[\\/](react|react-dom|scheduler)[\\/]/ },
            {
              name: 'query-vendor',
              test: /node_modules[\\/]@tanstack[\\/](react-)?query-core[\\/]|node_modules[\\/]@tanstack[\\/]react-query[\\/]/,
            },
            { name: 'router-vendor', test: /node_modules[\\/]react-router(-dom)?[\\/]/ },
          ],
        },
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
      '@shared': path.resolve(import.meta.dirname, './src/shared'),
      '@modules': path.resolve(import.meta.dirname, './src/modules'),
    },
  },
  server: {
    proxy: {
      '/authentik': {
        target: 'http://localhost:9000',
        changeOrigin: true,
        rewrite: (p) => p.replace(/^\/authentik/, ''),
      },
      ...(otlpTarget ? {
            '/otlp': {
              target: otlpTarget,
              changeOrigin: true,
              rewrite: (p) => p.replace(/^\/otlp/, ''),
            },
          } : {}),
    },
  },
});
