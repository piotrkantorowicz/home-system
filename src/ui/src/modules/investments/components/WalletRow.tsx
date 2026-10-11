import {
  Badge,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { Ellipsis } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { formatDate } from '../lib/dates';

import type { Wallet } from '../types';

export interface WalletRowProps {
  wallet: Wallet;
  /** Owner line for wallets that are not the caller's own; `null` for their own. */
  ownerLabel: string | null;
  busy: boolean;
  onEdit: () => void;
  onToggleArchive: () => void;
}

/** One wallet: name, institution and currency, owner and a private chip, and a ⋯ menu. */
export function WalletRow({ wallet, ownerLabel, busy, onEdit, onToggleArchive }: WalletRowProps) {
  const { t, i18n } = useTranslation('investments');
  const { money } = useFormat();
  const meta = [t(`institution_${wallet.institution}`), wallet.currency, ownerLabel].filter(
    Boolean,
  );

  return (
    <li className="border-border flex items-center gap-3 rounded-xl border p-3">
      <div className="min-w-0 flex-1">
        <div className="text-body flex flex-wrap items-center gap-2 font-semibold break-words">
          <Link to={`/investments/wallets/${wallet.id}`} className="hover:underline">
            {wallet.name}
          </Link>
          {wallet.visibility === 'Private' && <Badge variant="secondary">{t('private')}</Badge>}
        </div>
        <p className="text-muted-foreground text-meta">{meta.join(' · ')}</p>
      </div>
      <div className="text-right">
        <p className="tnum font-semibold">
          {wallet.latestBalance === null
            ? '—'
            : money(wallet.latestBalance, { currency: wallet.currency })}
        </p>
        <p className="text-muted-foreground text-meta">
          {wallet.latestAsOf ? formatDate(wallet.latestAsOf, i18n.language) : t('no_balance_yet')}
        </p>
      </div>
      {wallet.isArchived ? (
        <Button
          variant="outline"
          size="sm"
          disabled={busy}
          aria-label={t('restore_named', { name: wallet.name })}
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
              aria-label={t('wallet_actions', { name: wallet.name })}
            >
              <Ellipsis className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onSelect={onEdit}>{t('edit')}</DropdownMenuItem>
            <DropdownMenuItem disabled={busy} onSelect={onToggleArchive}>
              {t('archive')}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </li>
  );
}
