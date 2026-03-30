import { AuthCallback } from '@shared/auth/AuthCallback';
import { ProtectedRoute } from '@shared/auth/ProtectedRoute';
import { SilentRenew } from '@shared/auth/SilentRenew';
import { AppShell } from '@shared/components/layout/AppShell';
import { getModules } from '@shared/lib/module-registry';
import { Suspense } from 'react';
import { createBrowserRouter, Outlet } from 'react-router-dom';

import SystemDashboard from './SystemDashboard';

import type { RouteObject } from 'react-router-dom';

function SuspenseWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-[50vh] items-center justify-center">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-current border-r-transparent" />
        </div>
      }
    >
      {children}
    </Suspense>
  );
}

function buildModuleRoutes(): RouteObject[] {
  return getModules().map((mod) => ({
    path: mod.basePath.replace(/^\//, ''),
    element: <Outlet />,
    children: mod.routes.map((route) => ({
      ...route,
      element: route.Component ? (
        <SuspenseWrapper>
          <route.Component />
        </SuspenseWrapper>
      ) : (
        route.element
      ),
      Component: null,
    })),
  }));
}

export function createRouter() {
  return createBrowserRouter([
    // Auth routes (public)
    {
      path: '/callback',
      element: <AuthCallback />,
    },
    {
      path: '/silent-renew',
      element: <SilentRenew />,
    },
    // Protected routes
    {
      element: (
        <ProtectedRoute>
          <AppShell />
        </ProtectedRoute>
      ),
      children: [
        {
          path: '/',
          element: <SystemDashboard />,
        },
        ...buildModuleRoutes(),
      ],
    },
  ]);
}
