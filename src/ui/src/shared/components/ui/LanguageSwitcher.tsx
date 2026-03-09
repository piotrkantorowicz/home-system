import { useTranslation } from 'react-i18next';
import { Button } from './Button';

export function LanguageSwitcher() {
  const { i18n } = useTranslation();

  const toggleLanguage = () => {
    const newLang = i18n.language === 'en' ? 'pl' : 'en';
    i18n.changeLanguage(newLang);
  };

  return (
    <Button
      variant="ghost"
      size="sm"
      onClick={toggleLanguage}
      className="w-12 font-semibold tracking-wide"
    >
      {(i18n.language || 'en').toUpperCase()}
    </Button>
  );
}
