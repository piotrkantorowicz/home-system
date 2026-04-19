import { Sheet, SheetContent } from '@shared/components/ui';
import { useState } from 'react';
import { Outlet } from 'react-router-dom';

import { Header } from './Header';
import { Sidebar } from './Sidebar';

export function AppShell() {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const closeSidebar = () => {
    setSidebarOpen(false);
  };

  const openSidebar = () => {
    setSidebarOpen(true);
  };

  return (
    <div className="flex h-screen overflow-hidden">
      {/* Desktop sidebar — always visible on lg+ */}
      <div className="hidden lg:block">
        <Sidebar />
      </div>

      {/* Mobile sidebar — drawer */}
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
