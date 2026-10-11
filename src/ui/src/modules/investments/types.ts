import type { components } from './api/generated/schema';

export type Wallet = components['schemas']['WalletDto'];

export const WALLET_KINDS = [
  'Brokerage',
  'IKE',
  'IKZE',
  'PPK',
  'TreasuryBonds',
  'Savings',
  'Cash',
] as const;
export type WalletKind = (typeof WALLET_KINDS)[number];

export const WALLET_INSTITUTIONS = [
  'XTB',
  'Saxo',
  'Bossa',
  'MBank',
  'PKOBP',
  'Allianz',
  'Other',
] as const;
export type WalletInstitution = (typeof WALLET_INSTITUTIONS)[number];

export const WALLET_CURRENCIES = ['PLN', 'EUR', 'USD'] as const;
export type WalletCurrency = (typeof WALLET_CURRENCIES)[number];

export const WALLET_VISIBILITIES = ['Shared', 'Private'] as const;
export type WalletVisibility = (typeof WALLET_VISIBILITIES)[number];

export interface NewWallet {
  name: string;
  institution: WalletInstitution;
  kind: WalletKind;
  currency: WalletCurrency;
  visibility: WalletVisibility;
  ownerPersonId: string;
}
