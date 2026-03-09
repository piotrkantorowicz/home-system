import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import type { AppModule } from './module-registry';

import en from '../locales/en.json';
import pl from '../locales/pl.json';

const STORAGE_KEY = 'home-system-lang';

export function initI18n(modules: readonly AppModule[]) {
  const storedLang = localStorage.getItem(STORAGE_KEY);
  const defaultLang = storedLang === 'en' || storedLang === 'pl' ? storedLang : 'en';

  // Start with shared common translations
  const resources: Record<string, Record<string, unknown>> = {
    en: { translation: en as unknown as Record<string, unknown> },
    pl: { translation: pl as unknown as Record<string, unknown> },
  };

  // Merge module translations into the main 'translation' namespace
  for (const mod of modules) {
    for (const [lang, namespaces] of Object.entries(mod.i18nResources)) {
      if (!resources[lang]) resources[lang] = {};
      const existing = (resources[lang].translation ?? {}) as Record<string, unknown>;
      for (const [, translations] of Object.entries(namespaces)) {
        Object.assign(existing, translations);
      }
      resources[lang].translation = existing;
    }
  }

  i18n.use(initReactI18next).init({
    lng: defaultLang,
    resources,
    fallbackLng: 'en',
    interpolation: { escapeValue: false },
    react: { useSuspense: false },
  });

  i18n.on('languageChanged', (lng) => {
    localStorage.setItem(STORAGE_KEY, lng);
  });
}

export default i18n;
