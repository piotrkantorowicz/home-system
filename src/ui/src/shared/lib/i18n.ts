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

  const enResources: Record<string, Record<string, unknown>> = {};
  const plResources: Record<string, Record<string, unknown>> = {};

  // For each module: register every declared namespace as its own resource bundle,
  // and also deep-merge into the default `translation` namespace so callers using
  // `useTranslation()` (no namespace) keep resolving keys from any module.
  for (const mod of modules) {
    for (const [ns, content] of Object.entries(mod.i18nResources.en)) {
      enResources[ns] = content as Record<string, unknown>;
      deepMerge(enTranslation, content as Record<string, unknown>);
    }
    for (const [ns, content] of Object.entries(mod.i18nResources.pl)) {
      plResources[ns] = content as Record<string, unknown>;
      deepMerge(plTranslation, content as Record<string, unknown>);
    }
  }

  void i18n.use(initReactI18next).init({
    lng: defaultLang,
    initImmediate: false,
    resources: {
      en: { translation: enTranslation, ...enResources },
      pl: { translation: plTranslation, ...plResources },
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
