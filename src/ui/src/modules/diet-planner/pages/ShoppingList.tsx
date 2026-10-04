import {
  rowKey,
  useLocalShoppingChecks,
} from '@modules/diet-planner/api/hooks/useLocalShoppingChecks';
import {
  useClearShoppingChecks,
  useSetShoppingCheck,
  useShoppingList,
} from '@modules/diet-planner/api/hooks/useShoppingList';
import { useHousehold } from '@modules/household';
import {
  Banner,
  Button,
  Checkbox,
  DatePicker,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
  Label,
  PageContainer,
  PageHeader,
  Popover,
  PopoverContent,
  PopoverTrigger,
  Skeleton,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useFormat } from '@shared/hooks/useFormat';
import { CalendarDays, Clipboard, Download, Ellipsis, FileJson, ShoppingCart } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { ShoppingListItemDto } from '@modules/diet-planner/api/hooks/useShoppingList';

function formatLocalDate(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${String(year)}-${month}-${day}`;
}

function getDefaultRange() {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const day = today.getDay();
  const diff = today.getDate() - day + (day === 0 ? -6 : 1);
  const weekStart = new Date(today);
  weekStart.setDate(diff);
  const weekEnd = new Date(weekStart);
  weekEnd.setDate(weekEnd.getDate() + 6);
  return { from: formatLocalDate(weekStart), to: formatLocalDate(weekEnd) };
}

function formatAmount(value: number) {
  return Math.abs(value % 1) < 0.005 ? value.toFixed(0) : value.toFixed(2);
}

function downloadBlob(filename: string, content: string, mime: string) {
  const blob = new Blob([content], { type: mime });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
}

function csvEscape(value: string) {
  if (value.includes('"') || value.includes(',') || value.includes('\n')) {
    return `"${value.replace(/"/g, '""')}"`;
  }
  return value;
}

export default function ShoppingList() {
  const { t } = useTranslation();
  const toast = useToast();
  const { quantity, dayRange } = useFormat();
  const { household } = useHousehold();
  const defaultRange = getDefaultRange();

  const [draftFrom, setDraftFrom] = useState(defaultRange.from);
  const [draftTo, setDraftTo] = useState(defaultRange.to);
  const [appliedRange, setAppliedRange] = useState(defaultRange);
  const [rangeOpen, setRangeOpen] = useState(false);

  const { data, isLoading, isError, refetch } = useShoppingList(appliedRange);
  const setCheck = useSetShoppingCheck(appliedRange);
  const clearChecks = useClearShoppingChecks(appliedRange);

  const local = useLocalShoppingChecks(household?.id ?? 'none', appliedRange.from, appliedRange.to);
  // A check the API refused stays visible, marked as saved on this device only.
  const items = (data ?? []).map((i) => {
    const kept = local.checks[rowKey(i.productId, i.unit)];
    return kept === undefined ? { ...i, isLocal: false } : { ...i, isChecked: kept, isLocal: true };
  });

  // Re-sync once per household and range: push kept checks, and drop each one the API accepts.
  const synced = useRef('');
  useEffect(() => {
    const scope = `${household?.id ?? 'none'}|${appliedRange.from}|${appliedRange.to}`;
    if (!data || synced.current === scope) return;
    synced.current = scope;
    for (const [k, isChecked] of Object.entries(local.checks)) {
      const [productId = '', unit = ''] = k.split('|');
      setCheck.mutateAsync({ productId, unit, isChecked }).then(
        () => {
          local.clear(productId, unit);
        },
        () => undefined,
      );
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- REASON: run once per scope when its data first arrives
  }, [data]);
  const todo = items.filter((i) => !i.isChecked);
  const bought = items.filter((i) => i.isChecked);
  const rangeInvalid = !draftFrom || !draftTo || draftFrom > draftTo;

  const handleApply = () => {
    if (rangeInvalid) return;
    setAppliedRange({ from: draftFrom, to: draftTo });
    setRangeOpen(false);
  };

  const toggle = (item: ShoppingListItemDto, isChecked: boolean) => {
    setCheck.mutateAsync({ productId: item.productId, unit: item.unit, isChecked }).then(
      () => {
        local.clear(item.productId, item.unit);
      },
      () => {
        local.set(item.productId, item.unit, isChecked);
        toast.error(t('shopping_list.saved_locally_toast'));
      },
    );
  };

  const uncheckAll = () => {
    clearChecks.mutate(undefined, {
      onSuccess: () => {
        local.clearAll();
      },
      onError: () => {
        toast.error(t('shopping_list.check_failed'));
      },
    });
  };

  const handleCopy = async () => {
    if (items.length === 0) return;
    const text = items
      .map((item) => `${item.productName} — ${formatAmount(Number(item.totalAmount))} ${item.unit}`)
      .join('\n');
    try {
      await navigator.clipboard.writeText(text);
      toast.success(t('shopping_list.copied'));
    } catch {
      toast.error(t('shopping_list.copy_failed'));
    }
  };

  const handleExportCsv = () => {
    if (items.length === 0) return;
    const header = [
      t('shopping_list.product'),
      t('shopping_list.amount'),
      t('shopping_list.unit'),
    ].join(',');
    const rows = items.map((item) =>
      [
        csvEscape(item.productName),
        formatAmount(Number(item.totalAmount)),
        csvEscape(item.unit),
      ].join(','),
    );
    downloadBlob(
      `shopping-list-${appliedRange.from}_${appliedRange.to}.csv`,
      [header, ...rows].join('\n'),
      'text/csv;charset=utf-8',
    );
  };

  const handleExportJson = () => {
    if (items.length === 0) return;
    downloadBlob(
      `shopping-list-${appliedRange.from}_${appliedRange.to}.json`,
      JSON.stringify({ from: appliedRange.from, to: appliedRange.to, items }, null, 2),
      'application/json',
    );
  };

  const renderRow = (item: ShoppingListItemDto & { isLocal: boolean }) => (
    <li key={`${item.productId}-${item.unit}`}>
      <label
        className="hover:bg-accent/40 flex min-h-[52px] cursor-pointer items-center gap-3 rounded-lg px-3"
        data-testid="shopping-list-row"
      >
        <Checkbox
          className="size-[22px]"
          checked={item.isChecked}
          onChange={(e) => {
            toggle(item, e.target.checked);
          }}
        />
        <span
          className={`min-w-0 flex-1 font-medium ${item.isChecked ? 'text-muted-foreground line-through' : ''}`}
        >
          {item.productName}
          {item.isLocal ? (
            <span className="text-muted-foreground block text-xs font-normal no-underline">
              {t('shopping_list.saved_locally')}
            </span>
          ) : null}
        </span>
        <span className="text-muted-foreground tabular-nums">
          {quantity(Number(item.totalAmount), item.unit)}
        </span>
      </label>
    </li>
  );

  return (
    <PageContainer width="narrow">
      <PageHeader
        title={t('shopping_list.title')}
        subtitle={
          household
            ? t('shopping_list.subtitle_shared', {
                household: household.name,
                range: dayRange(appliedRange.from, appliedRange.to),
              })
            : t('shopping_list.subtitle', { range: dayRange(appliedRange.from, appliedRange.to) })
        }
        actions={
          <>
            <Popover open={rangeOpen} onOpenChange={setRangeOpen}>
              <PopoverTrigger asChild>
                <Button variant="outline" data-testid="shopping-list-range">
                  <CalendarDays className="mr-2 size-4" />
                  {dayRange(appliedRange.from, appliedRange.to)}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-64 space-y-3">
                <div>
                  <Label>{t('shopping_list.from')}</Label>
                  <DatePicker
                    testId="shopping-list-from"
                    value={draftFrom}
                    onChange={(v) => {
                      setDraftFrom(v ?? '');
                    }}
                  />
                </div>
                <div>
                  <Label>{t('shopping_list.to')}</Label>
                  <DatePicker
                    testId="shopping-list-to"
                    value={draftTo}
                    onChange={(v) => {
                      setDraftTo(v ?? '');
                    }}
                  />
                </div>
                {draftFrom && draftTo && draftFrom > draftTo ? (
                  <p role="alert" className="text-destructive text-sm">
                    {t('shopping_list.date_range_error')}
                  </p>
                ) : null}
                <Button className="w-full" onClick={handleApply} disabled={rangeInvalid}>
                  {t('shopping_list.apply')}
                </Button>
              </PopoverContent>
            </Popover>
            <Button
              variant="outline"
              disabled={items.length === 0}
              onClick={() => {
                void handleCopy();
              }}
              data-testid="shopping-list-copy"
            >
              <Clipboard className="mr-2 size-4" />
              {t('shopping_list.copy')}
            </Button>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  size="icon"
                  aria-label={t('shopping_list.more')}
                  data-testid="shopping-list-more"
                >
                  <Ellipsis className="size-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem
                  disabled={items.length === 0}
                  onSelect={handleExportCsv}
                  data-testid="shopping-list-export-csv"
                >
                  <Download />
                  {t('shopping_list.export_csv')}
                </DropdownMenuItem>
                <DropdownMenuItem
                  disabled={items.length === 0}
                  onSelect={handleExportJson}
                  data-testid="shopping-list-export-json"
                >
                  <FileJson />
                  {t('shopping_list.export_json')}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </>
        }
      />

      {isError ? (
        <Banner
          variant="error"
          onRetry={() => {
            void refetch();
          }}
          retryLabel={t('shopping_list.retry')}
        >
          {t('shopping_list.load_failed')}
        </Banner>
      ) : isLoading ? (
        <div className="space-y-2" aria-busy="true">
          <Skeleton className="h-[52px]" />
          <Skeleton className="h-[52px]" />
          <Skeleton className="h-[52px]" />
        </div>
      ) : items.length === 0 ? (
        <EmptyState
          icon={ShoppingCart}
          title={t('shopping_list.empty_title')}
          description={t('shopping_list.empty_desc')}
        />
      ) : (
        <>
          <div className="mb-4" data-testid="shopping-list-progress">
            <p className="text-muted-foreground mb-1.5 text-sm">
              {t('shopping_list.progress', { done: bought.length, total: items.length })}
            </p>
            <div
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={items.length}
              aria-valuenow={bought.length}
              aria-label={t('shopping_list.progress', { done: bought.length, total: items.length })}
              className="bg-muted h-1.5 overflow-hidden rounded-full"
            >
              <div
                className="bg-primary h-full"
                style={{ width: `${String((bought.length / items.length) * 100)}%` }}
              />
            </div>
          </div>
          <ul>{todo.map(renderRow)}</ul>
          {bought.length > 0 ? (
            <section
              className="mt-6"
              aria-label={t('shopping_list.bought', { count: bought.length })}
            >
              <div className="mb-1 flex items-center justify-between px-3">
                <h2 className="text-muted-foreground text-sm font-semibold">
                  {t('shopping_list.bought', { count: bought.length })}
                </h2>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={uncheckAll}
                  data-testid="shopping-list-uncheck-all"
                >
                  {t('shopping_list.uncheck_all')}
                </Button>
              </div>
              <ul>{bought.map(renderRow)}</ul>
            </section>
          ) : null}
        </>
      )}
    </PageContainer>
  );
}
