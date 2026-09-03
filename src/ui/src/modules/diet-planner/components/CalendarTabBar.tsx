import { cn } from '@shared/lib/utils';
import { CalendarDays, BarChart2, ShoppingCart, Upload } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export type CalendarTab = 'calendar' | 'nutrition' | 'shopping' | 'import';

interface CalendarTabBarProps {
  activeTab: CalendarTab;
  onTabChange: (tab: CalendarTab) => void;
}

const tabs = [
  { id: 'calendar' as const, icon: CalendarDays, translationKey: 'calendar.tab_calendar' },
  { id: 'nutrition' as const, icon: BarChart2, translationKey: 'calendar.tab_nutrition' },
  { id: 'shopping' as const, icon: ShoppingCart, translationKey: 'calendar.tab_shopping' },
  { id: 'import' as const, icon: Upload, translationKey: 'calendar.tab_import' },
];

export function CalendarTabBar({ activeTab, onTabChange }: CalendarTabBarProps) {
  const { t } = useTranslation();

  return (
    <div className="border-border mb-6 flex gap-1 border-b">
      {tabs.map((tab) => {
        const Icon = tab.icon;
        const isActive = activeTab === tab.id;
        return (
          <button
            key={tab.id}
            type="button"
            onClick={() => {
              onTabChange(tab.id);
            }}
            className={cn(
              'flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-medium transition-colors',
              isActive
                ? 'border-primary text-primary'
                : 'text-muted-foreground hover:border-border hover:text-foreground border-transparent',
            )}
          >
            <Icon className="h-4 w-4" />
            {t(tab.translationKey)}
          </button>
        );
      })}
    </div>
  );
}
