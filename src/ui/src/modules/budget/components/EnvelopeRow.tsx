import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { Ellipsis, Lock, Wallet } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { Account } from '../types';

export interface EnvelopeMonth {
  expenseCount: number;
  /** `null` is no limit; `"0.00"` is a real limit of zero. */
  limit: string | null;
}

export interface EnvelopeRowProps {
  account: Account;
  /** Counts and limit from the server's month summary; absent when the caller can't see them. */
  month: EnvelopeMonth | null;
  monthName: string;
  /** Extra line for personal envelopes that are not the caller's own. */
  ownerLabel: string | null;
  busy: boolean;
  onRename: () => void;
  onLimit: () => void;
  onToggleArchive: () => void;
}

/** One envelope: icon tile, name, this month's expense count, its monthly limit and a ⋯ menu. */
export function EnvelopeRow({
  account,
  month,
  monthName,
  ownerLabel,
  busy,
  onRename,
  onLimit,
  onToggleArchive,
}: EnvelopeRowProps) {
  const { t } = useTranslation('budget');
  const { money } = useFormat();
  const Icon = account.visibility === 'Personal' ? Lock : Wallet;
  const meta = [
    ownerLabel,
    month ? t('expenses_in_month', { count: month.expenseCount, month: monthName }) : null,
    month
      ? month.limit === null
        ? t('no_limit')
        : t('limit_per_month', { limit: money(month.limit) })
      : null,
  ].filter(Boolean);

  return (
    <li className="border-border flex items-center gap-3 rounded-xl border p-3">
      <span
        aria-hidden="true"
        className="bg-muted text-text-2 flex size-10 shrink-0 items-center justify-center rounded-lg"
      >
        <Icon className="size-5" />
      </span>
      <div className="min-w-0 flex-1">
        <p className="text-body font-semibold break-words">{account.name}</p>
        {meta.length > 0 && <p className="text-muted-foreground text-meta">{meta.join(' · ')}</p>}
      </div>
      {account.isArchived ? (
        <Button
          variant="outline"
          size="sm"
          disabled={busy}
          aria-label={t('restore_named', { name: account.name })}
          onClick={onToggleArchive}
        >
          {t('restore')}
        </Button>
      ) : (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              aria-label={t('envelope_actions', { name: account.name })}
            >
              <Ellipsis className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onSelect={onRename}>{t('rename')}</DropdownMenuItem>
            {month && (
              <DropdownMenuItem onSelect={onLimit}>
                {t(month.limit === null ? 'set_limit' : 'change_limit')}
              </DropdownMenuItem>
            )}
            <DropdownMenuItem disabled={busy} onSelect={onToggleArchive}>
              {t('archive')}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </li>
  );
}
