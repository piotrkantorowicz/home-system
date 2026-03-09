import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import type { AppModule } from './module-registry';

import sharedEn from '../locales/en.json';
import sharedPl from '../locales/pl.json';

const STORAGE_KEY = 'home-system-lang';

export function initI18n(modules: readonly AppModule[]) {
  const storedLang = localStorage.getItem(STORAGE_KEY);
  const defaultLang = storedLang === 'en' || storedLang === 'pl' ? storedLang : 'en';

  // Build fresh translation objects (spread to avoid mutating imported modules)
  const enTranslation: Record<string, unknown> = { ...sharedEn };
  const plTranslation: Record<string, unknown> = { ...sharedPl };

  // Merge each module's locale entries into the translation objects
  for (const mod of modules) {
    for (const nsContent of Object.values(mod.i18nResources.en)) {
      Object.assign(enTranslation, nsContent as Record<string, unknown>);
    }
    for (const nsContent of Object.values(mod.i18nResources.pl)) {
      Object.assign(plTranslation, nsContent as Record<string, unknown>);
    }
  }

  i18n.use(initReactI18next).init({
    lng: defaultLang,
    initImmediate: false,
    resources: {
      en: { translation: enTranslation },
      pl: { translation: plTranslation },
    },
    fallbackLng: 'en',
    interpolation: { escapeValue: false },
    react: { useSuspense: false },
  });

  i18n.on('languageChanged', (lng) => {
    localStorage.setItem(STORAGE_KEY, lng);
  });
}

export default i18n;
