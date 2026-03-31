import { useTheme } from '@shared/context/ThemeContext';
import { Sun, Moon, Monitor } from 'lucide-react';

import { Button } from './Button';

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  const cycleTheme = () => {
    const themes: ('light' | 'dark' | 'system')[] = ['light', 'dark', 'system'];
    const currentIndex = themes.indexOf(theme);
    const nextIndex = (currentIndex + 1) % themes.length;
    setTheme(themes[nextIndex] ?? 'system');
  };

  const getIcon = () => {
    switch (theme) {
      case 'light':
        return <Sun className="h-[18px] w-[18px] rotate-0 transition-transform duration-300" />;
      case 'dark':
        return <Moon className="h-[18px] w-[18px] rotate-0 transition-transform duration-300" />;
      case 'system':
        return <Monitor className="h-[18px] w-[18px] rotate-0 transition-transform duration-300" />;
    }
  };

  const getLabel = () => {
    switch (theme) {
      case 'light':
        return 'Light';
      case 'dark':
        return 'Dark';
      case 'system':
        return 'System';
    }
  };

  return (
    <Button
      variant="ghost"
      size="sm"
      onClick={cycleTheme}
      title={`Theme: ${getLabel()}`}
      data-testid="theme-toggle"
      className="gap-2"
    >
      {getIcon()}
      <span className="hidden text-sm sm:inline">{getLabel()}</span>
    </Button>
  );
}
