import { Sun, Moon, Monitor } from 'lucide-react';
import { useTheme } from '@shared/context/ThemeContext';
import { Button } from './Button';

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  const cycleTheme = () => {
    const themes: Array<'light' | 'dark' | 'system'> = ['light', 'dark', 'system'];
    const currentIndex = themes.indexOf(theme);
    const nextIndex = (currentIndex + 1) % themes.length;
    setTheme(themes[nextIndex]);
  };

  const getIcon = () => {
    switch (theme) {
      case 'light':
        return <Sun className="h-[18px] w-[18px] transition-transform duration-300 rotate-0" />;
      case 'dark':
        return <Moon className="h-[18px] w-[18px] transition-transform duration-300 rotate-0" />;
      case 'system':
        return <Monitor className="h-[18px] w-[18px] transition-transform duration-300 rotate-0" />;
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
      <span className="hidden sm:inline text-sm">{getLabel()}</span>
    </Button>
  );
}
