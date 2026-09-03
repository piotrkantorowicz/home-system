import { useNotificationStream } from '@modules/notifications/api/hooks/useNotificationStream';
import { useAuth } from 'react-oidc-context';
import { Outlet } from 'react-router-dom';

import { BottomTabBar } from './BottomTabBar';
import { Header } from './Header';
import { IconRail } from './IconRail';

export function AppShell() {
  const auth = useAuth();

  useNotificationStream(auth.isAuthenticated);

  return (
    <div className="flex h-screen overflow-hidden">
      {/* Desktop: 76px icon rail. Below md it collapses to the bottom tab bar. */}
      <div className="hidden md:block">
        <IconRail />
      </div>

      <div className="flex flex-1 flex-col overflow-hidden">
        <Header />
        <main className="ambient-bg flex-1 overflow-auto pb-20 md:pb-0">
          <div className="relative z-10">
            <Outlet />
          </div>
        </main>
      </div>

      <BottomTabBar />
    </div>
  );
}
