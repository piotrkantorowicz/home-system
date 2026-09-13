import { HouseholdProvider, HouseholdRequired } from '@modules/household';
import { AuthCallback } from '@shared/auth/AuthCallback';
import { ProtectedRoute } from '@shared/auth/ProtectedRoute';
import { SilentRenew } from '@shared/auth/SilentRenew';
import { AppShell } from '@shared/components/layout/AppShell';
import { getModules } from '@shared/lib/module-registry';
import { createBrowserRouter, Outlet } from 'react-router-dom';

import RootRedirect from './RootRedirect';
import { SuspenseWrapper } from './SuspenseWrapper';

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
    },
    {
      path: '/silent-renew',
      element: <SilentRenew />,
    },
    // Protected routes
    {
      element: (
        <ProtectedRoute>
          <HouseholdProvider>
            <AppShell />
          </HouseholdProvider>
        </ProtectedRoute>
      ),
      children: [
        {
          path: '/',
          element: (
            <HouseholdRequired>
              <RootRedirect />
            </HouseholdRequired>
          ),
        },
        ...buildModuleRoutes(),
      ],
    },
  ]);
}
