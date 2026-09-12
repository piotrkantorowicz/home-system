import { createContext, use } from 'react';

export const ModuleLabelsContext = createContext<Readonly<Record<string, string>>>({});

export function useModuleLabels() {
  return use(ModuleLabelsContext);
}
