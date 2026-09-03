import { useShoppingList } from '@modules/diet-planner/api/hooks/useShoppingList';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  DatePicker,
  EmptyState,
  Label,
  Pagination,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Clipboard, Download, FileJson, ShoppingCart } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

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
  const defaultRange = getDefaultRange();

  const [draftFrom, setDraftFrom] = useState(defaultRange.from);
  const [draftTo, setDraftTo] = useState(defaultRange.to);
  const [appliedRange, setAppliedRange] = useState(defaultRange);
  const [tablePage, setTablePage] = useState(1);
  const [tablePageSize, setTablePageSize] = useState(25);

  const { data, isLoading } = useShoppingList({
    from: appliedRange.from,
    to: appliedRange.to,
  });

  const items = useMemo(() => data ?? [], [data]);

  const pagedItems = useMemo(() => {
    const start = (tablePage - 1) * tablePageSize;
    return items.slice(start, start + tablePageSize);
  }, [items, tablePage, tablePageSize]);

  const handleApply = () => {
    if (draftFrom && draftTo && draftFrom <= draftTo) {
      setAppliedRange({ from: draftFrom, to: draftTo });
      setTablePage(1);
    }
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

  return (
    <div className="animate-fade-in-up mx-auto max-w-5xl p-8 lg:p-10">
      <div className="mb-8">
        <div className="mb-3 flex items-center gap-3">
          <div className="rounded-xl bg-emerald-500/10 p-2.5">
            <ShoppingCart className="h-6 w-6 text-emerald-600 dark:text-emerald-400" />
          </div>
          <h1 className="text-3xl font-bold tracking-tight">{t('shopping_list.title')}</h1>
        </div>
        <p className="text-muted-foreground">{t('shopping_list.subtitle')}</p>
      </div>

      <Card className="mb-6">
        <CardContent className="pt-6">
          <div className="flex flex-wrap items-end gap-4">
            <div>
              <Label>{t('shopping_list.from')}</Label>
              <DatePicker
                testId="shopping-list-from"
                value={draftFrom}
                onChange={(v) => {
                  setDraftFrom(v ?? '');
                }}
                className="w-44"
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
                className="w-44"
              />
            </div>
            <Button onClick={handleApply} disabled={!draftFrom || !draftTo || draftFrom > draftTo}>
              {t('shopping_list.apply')}
            </Button>
          </div>
          {draftFrom && draftTo && draftFrom > draftTo && (
            <p role="alert" className="text-destructive mt-2 text-sm">
              {t('shopping_list.date_range_error')}
            </p>
          )}
        </CardContent>
      </Card>

      {isLoading ? (
        <div className="text-muted-foreground flex items-center justify-center py-16">
          {t('common.loading')}
        </div>
      ) : items.length === 0 ? (
        <EmptyState
          icon={ShoppingCart}
          title={t('shopping_list.empty_title')}
          description={t('shopping_list.empty_desc')}
        />
      ) : (
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-3">
            <CardTitle className="text-base">
              {t('shopping_list.items_count', { count: items.length })}
            </CardTitle>
            <div className="flex flex-wrap gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  void handleCopy();
                }}
                data-testid="shopping-list-copy"
              >
                <Clipboard className="mr-2 h-4 w-4" />
                {t('shopping_list.copy')}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={handleExportCsv}
                data-testid="shopping-list-export-csv"
              >
                <Download className="mr-2 h-4 w-4" />
                {t('shopping_list.export_csv')}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={handleExportJson}
                data-testid="shopping-list-export-json"
              >
                <FileJson className="mr-2 h-4 w-4" />
                {t('shopping_list.export_json')}
              </Button>
            </div>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow className="bg-muted/30">
                  <TableHead>{t('shopping_list.product')}</TableHead>
                  <TableHead className="text-right">{t('shopping_list.amount')}</TableHead>
                  <TableHead>{t('shopping_list.unit')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {pagedItems.map((item) => (
                  <TableRow key={`${item.productId}-${item.unit}`}>
                    <TableCell className="font-medium">{item.productName}</TableCell>
                    <TableCell className="text-right tabular-nums">
                      {formatAmount(Number(item.totalAmount))}
                    </TableCell>
                    <TableCell>{item.unit}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
          <div className="px-6 pb-4">
            <Pagination
              page={tablePage}
              pageSize={tablePageSize}
              totalCount={items.length}
              onPageChange={setTablePage}
              onPageSizeChange={setTablePageSize}
            />
          </div>
        </Card>
      )}
    </div>
  );
}
