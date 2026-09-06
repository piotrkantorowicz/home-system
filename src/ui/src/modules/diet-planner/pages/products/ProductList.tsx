import { useProducts, useDeleteProduct } from '@modules/diet-planner/api/hooks/useProducts';
import { unitLabel } from '@modules/diet-planner/unitLabel';
import {
  Banner,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  EmptyState,
  Pagination,
  SegmentedControl,
  Skeleton,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { cn } from '@shared/lib/utils';
import { MoreVertical, Package, Pencil, Plus, Search, Trash2 } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

type ViewMode = 'table' | 'cards';

interface Row {
  id: string;
  name: string;
  caloriesPer100g: number | null;
  proteinPer100g: number | null;
  carbsPer100g: number | null;
  fatPer100g: number | null;
  fiberPer100g?: number | null;
  defaultUnit: string;
  isOwner: boolean;
}

const macroClass = { protein: 'text-protein', carbs: 'text-carbs', fat: 'text-fat' } as const;

function dominant(p: Row): 'protein' | 'carbs' | 'fat' {
  const v = { protein: p.proteinPer100g ?? 0, carbs: p.carbsPer100g ?? 0, fat: p.fatPer100g ?? 0 };
  return (['protein', 'carbs', 'fat'] as const).reduce((a, b) => (v[b] > v[a] ? b : a));
}

function isIncomplete(p: Row): boolean {
  return (
    p.caloriesPer100g === null ||
    p.proteinPer100g === null ||
    p.carbsPer100g === null ||
    p.fatPer100g === null
  );
}

const fmt = (v: number | null | undefined): string => (v ?? 0).toFixed(1);

export default function ProductList() {
  const { t } = useTranslation();
  const toast = useToast();
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [onlyMine, setOnlyMine] = useState(false);
  const [onlyIncomplete, setOnlyIncomplete] = useState(false);
  const [view, setView] = useState<ViewMode>('table');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const { data, isLoading, error } = useProducts({
    search: debouncedSearch,
    onlyMine,
    page,
    pageSize,
  });
  const deleteMutation = useDeleteProduct();

  const searchTimerRef = useRef<ReturnType<typeof setTimeout>>(undefined);
  useEffect(
    () => () => {
      clearTimeout(searchTimerRef.current);
    },
    [],
  );

  const handleSearchChange = (value: string) => {
    setSearch(value);
    clearTimeout(searchTimerRef.current);
    searchTimerRef.current = setTimeout(() => {
      setDebouncedSearch(value);
      setPage(1);
    }, 300);
  };

  const items = useMemo(() => (data?.items ?? []) as Row[], [data]);
  const incompleteCount = useMemo(() => items.filter(isIncomplete).length, [items]);
  const rows = onlyIncomplete ? items.filter(isIncomplete) : items;

  const handleDelete = async () => {
    if (!deleteId) return;
    try {
      await deleteMutation.mutateAsync({ id: deleteId });
      setDeleteId(null);
    } catch {
      toast.error(t('common.error'));
    }
  };

  return (
    <div className="animate-fade-in flex flex-col gap-6 px-4 py-6 md:px-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[26px] font-bold">{t('products.title')}</h1>
          <p className="text-muted-foreground mt-1 text-sm">
            {t('products.count', { count: data?.totalCount ?? 0 })}
          </p>
        </div>
        <Button size="xl" asChild>
          <Link to="/diet-planner/products/new">
            <Plus className="size-4" />
            {t('products.add_product')}
          </Link>
        </Button>
      </div>

      {/* Filter strip */}
      <div className="border-border bg-card flex flex-wrap items-center gap-2.5 rounded-[18px] border p-3.5">
        <div className="bg-secondary border-border focus-within:ring-primary flex h-[38px] min-w-[180px] flex-1 items-center gap-2 rounded-[12px] border px-3 focus-within:ring-2">
          <Search className="text-muted-foreground size-[15px] shrink-0" strokeWidth={2} />
          <input
            value={search}
            onChange={(e) => {
              handleSearchChange(e.target.value);
            }}
            placeholder={t('products.search_placeholder')}
            aria-label={t('products.search_placeholder')}
            className="text-foreground placeholder:text-muted-foreground w-full bg-transparent text-[13px] outline-none"
          />
        </div>

        <FilterChip
          active={onlyMine}
          onClick={() => {
            setOnlyMine((v) => !v);
            setPage(1);
          }}
        >
          {t('products.only_mine')}
        </FilterChip>
        {incompleteCount > 0 ? (
          <FilterChip
            active={onlyIncomplete}
            onClick={() => {
              setOnlyIncomplete((v) => !v);
            }}
          >
            {t('products.incomplete_chip', { count: incompleteCount })}
          </FilterChip>
        ) : null}

        <SegmentedControl
          className="ml-auto"
          label={t('products.view_label')}
          value={view}
          onChange={setView}
          options={[
            { value: 'table', label: t('products.view_table') },
            { value: 'cards', label: t('products.view_cards') },
          ]}
        />
      </div>

      {error ? <Banner variant="error">{error.message}</Banner> : null}

      {isLoading ? (
        <Skeleton className="h-[420px] w-full rounded-[22px]" />
      ) : rows.length === 0 ? (
        <EmptyState
          icon={Package}
          title={t('products.no_products_found')}
          description={debouncedSearch ? t('products.adjust_search') : t('products.start_creating')}
          action={
            debouncedSearch
              ? undefined
              : { label: t('products.add_first_product'), href: '/diet-planner/products/new' }
          }
        />
      ) : view === 'table' ? (
        <div className="border-border bg-card overflow-x-auto rounded-[22px] border" role="table">
          <div className="min-w-[720px]">
            <div
              role="row"
              className="bg-secondary text-muted-foreground grid grid-cols-[2.2fr_1fr_0.8fr_0.8fr_0.8fr_0.8fr_44px] gap-3 px-5 py-2.5 text-[10.5px] font-semibold uppercase"
            >
              <span role="columnheader">{t('products.table.name')}</span>
              <span role="columnheader" className="text-right">
                {t('products.table.calories')}
              </span>
              <span role="columnheader" className="text-right">
                {t('products.table.protein')}
              </span>
              <span role="columnheader" className="text-right">
                {t('products.table.carbs')}
              </span>
              <span role="columnheader" className="text-right">
                {t('products.table.fat')}
              </span>
              <span role="columnheader" className="text-right">
                {t('products.table.fiber')}
              </span>
              <span />
            </div>
            {rows.map((p) => {
              const dom = dominant(p);
              const incomplete = isIncomplete(p);
              return (
                <div
                  key={p.id}
                  role="row"
                  aria-label={p.name}
                  className={cn(
                    'border-border grid grid-cols-[2.2fr_1fr_0.8fr_0.8fr_0.8fr_0.8fr_44px] items-center gap-3 border-t px-5 py-3.5 text-[13px]',
                  )}
                  style={
                    incomplete
                      ? {
                          background: 'color-mix(in oklab, var(--color-carbs) 7%, transparent)',
                        }
                      : undefined
                  }
                >
                  <span className="flex min-w-0 items-center gap-2">
                    <Link
                      to={`/diet-planner/products/${p.id}`}
                      className="truncate font-semibold hover:underline"
                    >
                      {p.name}
                    </Link>
                    {incomplete ? <IncompleteBadge label={t('products.incomplete_badge')} /> : null}
                    <span className="text-muted-foreground text-[11.5px]">
                      {unitLabel(p.defaultUnit, t)}
                    </span>
                  </span>
                  <span className="tnum text-right font-semibold">{fmt(p.caloriesPer100g)}</span>
                  <MacroCell value={p.proteinPer100g} on={dom === 'protein'} macro="protein" />
                  <MacroCell value={p.carbsPer100g} on={dom === 'carbs'} macro="carbs" />
                  <MacroCell value={p.fatPer100g} on={dom === 'fat'} macro="fat" />
                  <span className="text-text-2 tnum text-right">{fmt(p.fiberPer100g)}</span>
                  <RowMenu
                    product={p}
                    onDelete={() => {
                      setDeleteId(p.id);
                    }}
                  />
                </div>
              );
            })}
          </div>
        </div>
      ) : (
        <div className="grid gap-[18px] sm:grid-cols-2 lg:grid-cols-3" role="list">
          {rows.map((p) => (
            <ProductCardItem
              key={p.id}
              product={p}
              onDelete={() => {
                setDeleteId(p.id);
              }}
            />
          ))}
        </div>
      )}

      {data ? (
        <Pagination
          page={page}
          pageSize={pageSize}
          totalCount={data.totalCount}
          onPageChange={setPage}
          onPageSizeChange={(size) => {
            setPageSize(size);
            setPage(1);
          }}
        />
      ) : null}

      <Dialog
        open={deleteId !== null}
        onOpenChange={(open) => {
          if (!open) setDeleteId(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('products.delete_dialog.title')}</DialogTitle>
            <DialogDescription>{t('products.delete_dialog.description')}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setDeleteId(null);
              }}
            >
              {t('common.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                void handleDelete();
              }}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending
                ? t('products.delete_dialog.deleting')
                : t('products.delete_dialog.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function FilterChip({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        'inline-flex h-8 items-center gap-1.5 rounded-full px-3 text-[12px] font-semibold transition-colors',
        active
          ? 'bg-accent text-accent-foreground'
          : 'bg-secondary border-border text-text-2 hover:text-foreground border',
      )}
    >
      {children}
      {active ? <span aria-hidden>×</span> : null}
    </button>
  );
}

function MacroCell({
  value,
  on,
  macro,
}: {
  value: number | null | undefined;
  on: boolean;
  macro: 'protein' | 'carbs' | 'fat';
}) {
  return (
    <span
      className={cn('tnum text-right', on ? cn('font-semibold', macroClass[macro]) : 'text-text-2')}
    >
      {fmt(value)}
    </span>
  );
}

function IncompleteBadge({ label }: { label: string }) {
  return (
    <span
      className="rounded-[6px] px-1.5 py-0.5 text-[10px] font-bold text-[var(--color-carbs)]"
      style={{ background: 'color-mix(in oklab, var(--color-carbs) 22%, transparent)' }}
    >
      {label}
    </span>
  );
}

function RowMenu({ product, onDelete }: { product: Row; onDelete: () => void }) {
  const { t } = useTranslation();
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label={t('common.actions')}
          className="text-muted-foreground hover:text-foreground grid size-8 place-items-center rounded-[10px]"
        >
          <MoreVertical className="size-4" />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem asChild>
          <Link to={`/diet-planner/products/${product.id}`}>{t('common.view')}</Link>
        </DropdownMenuItem>
        {product.isOwner ? (
          <>
            <DropdownMenuItem asChild>
              <Link to={`/diet-planner/products/${product.id}/edit`}>
                <Pencil className="size-4" />
                {t('common.edit')}
              </Link>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              onSelect={onDelete}
              className="text-destructive focus:text-destructive"
            >
              <Trash2 className="size-4" />
              {t('common.delete')}
            </DropdownMenuItem>
          </>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function ProductCardItem({ product, onDelete }: { product: Row; onDelete: () => void }) {
  const { t } = useTranslation();
  const incomplete = isIncomplete(product);
  const chips: { key: 'protein' | 'carbs' | 'fat'; label: string }[] = [
    { key: 'protein', label: 'P' },
    { key: 'carbs', label: 'C' },
    { key: 'fat', label: 'F' },
  ];
  const val = {
    protein: product.proteinPer100g,
    carbs: product.carbsPer100g,
    fat: product.fatPer100g,
  };

  return (
    <div
      role="listitem"
      aria-label={product.name}
      className="border-border bg-card flex flex-col gap-3 rounded-[22px] border p-[18px] shadow-sm"
    >
      <div className="flex items-start justify-between gap-2">
        <Link
          to={`/diet-planner/products/${product.id}`}
          className="text-[14px] font-bold hover:underline"
        >
          {product.name}
        </Link>
        <RowMenu product={product} onDelete={onDelete} />
      </div>
      {incomplete ? <IncompleteBadge label={t('products.incomplete_badge')} /> : null}
      <div className="numeral text-[20px] font-bold">
        {fmt(product.caloriesPer100g)}
        <span className="text-muted-foreground ml-1 text-[11px] font-medium">kcal / 100 g</span>
      </div>
      <div className="grid grid-cols-3 gap-2">
        {chips.map((c) => (
          <div
            key={c.key}
            className="rounded-[9px] p-2 text-center"
            style={{
              background: `color-mix(in oklab, var(--color-${c.key}) 12%, transparent)`,
            }}
          >
            <div className="tnum text-[13px] font-bold">{fmt(val[c.key])}</div>
            <div className="text-muted-foreground text-[9.5px]">{c.label}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
