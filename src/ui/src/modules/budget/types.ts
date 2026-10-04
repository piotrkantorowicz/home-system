import type { components } from './api/generated/schema';

export type BudgetSummary = components['schemas']['BudgetDto'];
export type Account = components['schemas']['AccountDto'];
export type AccountVisibility = 'Household' | 'Personal';
export const BUDGET_CURRENCIES = ['PLN', 'EUR', 'USD'] as const;
export type BudgetCurrency = (typeof BUDGET_CURRENCIES)[number];
export type Expense = components['schemas']['ExpenseDto'];
export type ExpenseRevision = components['schemas']['ExpenseRevisionDto'];
export type FundingSource = 'Individual' | 'HouseholdFunds';
export const EXPENSE_CATEGORIES = [
  'Groceries',
  'Housing',
  'Utilities',
  'Transport',
  'Health',
  'Leisure',
  'Education',
  'Other',
] as const;
export type ExpenseCategory = (typeof EXPENSE_CATEGORIES)[number];

export interface ExpenseFilters {
  page?: number;
  pageSize?: number;
  accountId?: string;
  category?: ExpenseCategory;
  amount?: string;
  from?: string;
  to?: string;
  excludeId?: string;
  includeVoided?: boolean;
}

export interface ExpenseInput {
  accountId: string;
  amount: string;
  occurredOn: string;
  category: ExpenseCategory;
  fundingSource: FundingSource | null;
  paidByPersonId: string | null;
  participantIds: string[];
  /** Optional short note (what the money was for); `null` or blank means none. */
  description?: string | null;
}

export type Settlement = components['schemas']['SettlementDto'];
export type Repayment = components['schemas']['RepaymentDto'];

export interface RepaymentInput {
  fromPersonId: string;
  toPersonId: string;
  amount: string;
  paidOn: string;
  note: string | null;
}
