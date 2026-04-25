import { Sheet, SheetContent } from '@shared/components/ui';
import { useSidebarCollapsed } from '@shared/hooks/useSidebarCollapsed';
import { useState } from 'react';
import { Outlet } from 'react-router-dom';

import { Header } from './Header';
import { Sidebar } from './Sidebar';

export function AppShell() {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [collapsed, setCollapsed] = useSidebarCollapsed();

  const closeSidebar = () => {
    setSidebarOpen(false);
  };

  const openSidebar = () => {
    setSidebarOpen(true);
  };

  const toggleCollapsed = () => {
    setCollapsed(!collapsed);
  };

  return (
    <div className="flex h-screen overflow-hidden">
      {/* Desktop sidebar — always visible on lg+ */}
      <div className="hidden lg:block">
        <Sidebar collapsed={collapsed} onToggleCollapsed={toggleCollapsed} />
      </div>

      {/* Mobile sidebar — drawer (always full-width regardless of collapsed pref) */}
      <Sheet open={sidebarOpen} onOpenChange={setSidebarOpen}>
        <SheetContent onClose={closeSidebar}>
          <Sidebar onClose={closeSidebar} />
        </SheetContent>
      </Sheet>

      <div className="flex flex-1 flex-col overflow-hidden">
        <Header onOpenSidebar={openSidebar} />
        <main className="ambient-bg flex-1 overflow-auto">
          <div className="relative z-10">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
