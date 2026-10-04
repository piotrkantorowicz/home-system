import { Badge, MoneyText } from '@shared/components/ui';
import {
  Car,
  GraduationCap,
  HeartPulse,
  Home,
  Receipt,
  ShoppingCart,
  Sun,
  Zap,
  type LucideIcon,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import type { Expense } from '../types';

const CATEGORY_ICONS: Record<string, LucideIcon> = {
  Groceries: ShoppingCart,
  Housing: Home,
  Utilities: Zap,
  Transport: Car,
  Health: HeartPulse,
  Leisure: Sun,
  Education: GraduationCap,
};

export interface ExpenseRowProps {
  expense: Expense;
  envelopeName: string;
  myPersonId: string | null;
}

/** One expense, description first (category when there is none), linking to its detail page. */
export function ExpenseRow({ expense: e, envelopeName, myPersonId }: ExpenseRowProps) {
  const { t } = useTranslation('budget');
  const Icon = CATEGORY_ICONS[e.category] ?? Receipt;
  const category = t(`categories.${e.category}`);
  const myShare = e.shares.find((s) => s.personId === myPersonId);
  const split =
    e.shares.length === 0
      ? null
      : myShare
        ? t('split_with_you')
        : t('split_ways', { count: e.shares.length });
  const meta = [
    category,
    envelopeName,
    e.paidByDisplayName ? t('row_paid', { name: e.paidByDisplayName }) : t('row_household'),
    split,
  ].filter(Boolean);

  return (
    <Link
      to={`/budget/expenses/${e.id}`}
      className="hover:bg-accent focus-visible:ring-ring flex min-h-16 items-center gap-3 rounded-lg px-2 py-2 focus-visible:ring-2 focus-visible:outline-none"
    >
      <span
        aria-hidden="true"
        className="bg-muted text-text-2 flex size-10 shrink-0 items-center justify-center rounded-lg"
      >
        <Icon className="size-5" />
      </span>
      <span className="min-w-0 flex-1">
        <span
          className={`text-body block truncate font-semibold ${e.isVoided ? 'line-through' : ''}`}
        >
          {e.description?.trim() ? e.description.trim() : category}
        </span>
        <span className="text-muted-foreground text-meta block truncate">{meta.join(' · ')}</span>
      </span>
      <span className="shrink-0 text-right">
        {e.isVoided && <Badge variant="destructive">{t('voided')}</Badge>}
        <MoneyText amount={e.amount} className="text-body block font-semibold" />
        <span className="text-muted-foreground text-meta block">
          {myShare ? (
            <>
              {t('your_share')} <MoneyText amount={myShare.amount} />
            </>
          ) : e.shares.length === 0 ? (
            t('not_split')
          ) : null}
        </span>
      </span>
    </Link>
  );
}
