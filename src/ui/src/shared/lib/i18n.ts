import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import sharedEn from '../locales/en.json';
import sharedPl from '../locales/pl.json';

import type { AppModule } from './module-registry';

const STORAGE_KEY = 'home-system-lang';

/**
 * Deep-merge source into target. For overlapping keys whose values are both
 * plain objects, the merge recurses instead of overwriting.
 */
function deepMerge(
  target: Record<string, unknown>,
  source: Record<string, unknown>,
): Record<string, unknown> {
  for (const key of Object.keys(source)) {
    const tVal = target[key];
    const sVal = source[key];

    if (
      tVal &&
      sVal &&
      typeof tVal === 'object' &&
      typeof sVal === 'object' &&
      !Array.isArray(tVal) &&
      !Array.isArray(sVal)
    ) {
      target[key] = deepMerge(
        { ...(tVal as Record<string, unknown>) },
        sVal as Record<string, unknown>,
      );
    } else {
      target[key] = sVal;
    }
  }
  return target;
}

export function initI18n(modules: readonly AppModule[]) {
  const storedLang = localStorage.getItem(STORAGE_KEY);
  const defaultLang = storedLang === 'en' || storedLang === 'pl' ? storedLang : 'en';

  // Build fresh translation objects (spread to avoid mutating imported modules)
  const enTranslation: Record<string, unknown> = { ...sharedEn };
  const plTranslation: Record<string, unknown> = { ...sharedPl };

  // Deep-merge each module's locale entries so overlapping keys like "common"
  // are merged recursively instead of being replaced.
  for (const mod of modules) {
    for (const nsContent of Object.values(mod.i18nResources.en)) {
      deepMerge(enTranslation, nsContent as Record<string, unknown>);
    }
    for (const nsContent of Object.values(mod.i18nResources.pl)) {
      deepMerge(plTranslation, nsContent as Record<string, unknown>);
    }
  }

  void i18n.use(initReactI18next).init({
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
