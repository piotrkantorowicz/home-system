import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from '@shared/auth/AuthProvider';
import { ThemeProvider } from '@shared/context/ThemeContext';
import { ErrorBoundary } from '@shared/components/ErrorBoundary';
import { queryClient } from '@shared/api/queryClient';
import { registerModule, getModules } from '@shared/lib/module-registry';
import { initI18n } from '@shared/lib/i18n';
import { dietPlannerModule } from '@modules/diet-planner';
import { createRouter } from './router';
import '../index.css';

// 1. Register all modules
registerModule(dietPlannerModule);

// 2. Init i18n with merged module translations
initI18n(getModules());

// 3. Build router with registered module routes
const router = createRouter();

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ErrorBoundary>
      <ThemeProvider>
        <QueryClientProvider client={queryClient}>
          <AuthProvider>
            <RouterProvider router={router} />
          </AuthProvider>
        </QueryClientProvider>
      </ThemeProvider>
    </ErrorBoundary>
  </React.StrictMode>
);
