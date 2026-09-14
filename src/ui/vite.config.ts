import path from 'path';

import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    rolldownOptions: {
      output: {
        codeSplitting: {
          groups: [
            { name: 'react-vendor', test: /node_modules[\\/](react|react-dom|scheduler)[\\/]/ },
            { name: 'query-vendor', test: /node_modules[\\/]@tanstack[\\/](react-)?query-core[\\/]|node_modules[\\/]@tanstack[\\/]react-query[\\/]/ },
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
    },
  },
});
