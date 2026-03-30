import { dietPlannerModule } from '@modules/diet-planner';
import { queryClient } from '@shared/api/queryClient';
import { AuthProvider } from '@shared/auth/AuthProvider';
import { AppErrorBoundary } from '@shared/components/ErrorBoundary';
import { ThemeProvider } from '@shared/context/ThemeContext';
import { initI18n } from '@shared/lib/i18n';
import { registerModule, getModules } from '@shared/lib/module-registry';
import { QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';

import { createRouter } from './router';
import '../index.css';

// 1. Register all modules
registerModule(dietPlannerModule);

// 2. Init i18n with merged module translations
initI18n(getModules());

// 3. Build router with registered module routes
const router = createRouter();

const rootElement = document.getElementById('root');
if (!rootElement) throw new Error('Root element #root not found in document');

ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <AppErrorBoundary>
      <ThemeProvider>
        <QueryClientProvider client={queryClient}>
          <AuthProvider>
            <RouterProvider router={router} />
          </AuthProvider>
        </QueryClientProvider>
      </ThemeProvider>
    </AppErrorBoundary>
  </React.StrictMode>,
);
