import { useNotificationStream } from '@modules/notifications/api/hooks/useNotificationStream';
import { usePreferenceEffects } from '@shared/hooks/usePreferences';
import { useEffect } from 'react';
import { useAuth } from 'react-oidc-context';
import { Outlet, useLocation } from 'react-router-dom';

import { BottomTabBar } from './BottomTabBar';
import { CommandPalette } from './CommandPalette';
import { Header } from './Header';
import { Sidebar } from './Sidebar';
import { getActiveModule, isSwitcherModule, LAST_MODULE_STORAGE_KEY } from './navModel';

export function AppShell() {
  const auth = useAuth();
  const location = useLocation();

  useNotificationStream(auth.isAuthenticated);
  usePreferenceEffects();

  // Remember the last module actually visited, so `/` can redirect back to it.
  useEffect(() => {
    const mod = getActiveModule(location.pathname);
    if (!mod || !isSwitcherModule(mod)) return;
    try {
      window.localStorage.setItem(LAST_MODULE_STORAGE_KEY, mod.name);
    } catch {
      // storage unavailable — "/" falls back to the first registered module
    }
  }, [location.pathname]);

  return (
    <div className="flex h-screen overflow-hidden">
      {/* Desktop: one 240px sidebar. Below md it gives way to the bottom tab bar and the
          mobile header (which holds the module switcher). */}
      <Sidebar />

      <div className="flex flex-1 flex-col overflow-hidden">
        <Header />
        <main className="ambient-bg flex-1 overflow-auto pb-20 md:pb-0">
          <div className="relative z-10">
            <Outlet />
          </div>
        </main>
      </div>

      <BottomTabBar />
      <CommandPalette />
    </div>
  );
}
