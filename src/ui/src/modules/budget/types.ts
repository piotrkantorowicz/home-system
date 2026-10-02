import type { components } from './api/generated/schema';

export type BudgetSummary = components['schemas']['BudgetDto'];
export type Account = components['schemas']['AccountDto'];
export type AccountVisibility = 'Household' | 'Personal';
export const BUDGET_CURRENCIES = ['PLN', 'EUR', 'USD'] as const;
export type BudgetCurrency = (typeof BUDGET_CURRENCIES)[number];
