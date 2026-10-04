import { HouseholdProvider, HouseholdRequired } from '@modules/household';
import { AuthCallback } from '@shared/auth/AuthCallback';
import { ProtectedRoute } from '@shared/auth/ProtectedRoute';
import { SilentRenew } from '@shared/auth/SilentRenew';
import { RouteError } from '@shared/components/RouteError';
import { AppShell } from '@shared/components/layout/AppShell';
import { getModules } from '@shared/lib/module-registry';
import { lazy } from 'react';
import { createBrowserRouter, Outlet } from 'react-router-dom';

import RootRedirect from './RootRedirect';
import { SuspenseWrapper } from './SuspenseWrapper';

const Preferences = lazy(() => import('./pages/Preferences'));

import type { RouteObject } from 'react-router-dom';

function buildModuleRoutes(): RouteObject[] {
  return getModules().map((mod) => ({
    path: mod.basePath.replace(/^\//, ''),
    element: (
      <HouseholdRequired>
        <Outlet />
      </HouseholdRequired>
    ),
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
      errorElement: <RouteError />,
    },
    {
      path: '/silent-renew',
      element: <SilentRenew />,
      errorElement: <RouteError />,
    },
    // Protected routes
    {
      errorElement: <RouteError />,
      element: (
        <ProtectedRoute>
          <HouseholdProvider>
            <AppShell />
          </HouseholdProvider>
        </ProtectedRoute>
      ),
      children: [
        // Pathless boundary: a failing page renders inside the shell, not instead of it.
        {
          errorElement: <RouteError />,
          children: [
            {
              path: '/',
              element: (
                <HouseholdRequired>
                  <RootRedirect />
                </HouseholdRequired>
              ),
            },
            {
              path: '/settings/app',
              element: (
                <SuspenseWrapper>
                  <Preferences />
                </SuspenseWrapper>
              ),
            },
            ...buildModuleRoutes(),
            { path: '*', element: <RouteError /> },
          ],
        },
      ],
    },
  ]);
}
