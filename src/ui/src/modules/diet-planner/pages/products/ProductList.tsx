import {
  type ProductSortKey,
  productOptions,
  useDeleteProduct,
  useProducts,
} from '@modules/diet-planner/api/hooks/useProducts';
import { VisibilityBadge } from '@modules/diet-planner/components/VisibilityBadge';
import { useListLocation } from '@modules/diet-planner/hooks/useListLocation';
import { unitLabel } from '@modules/diet-planner/unitLabel';
import { canWriteLibrary } from '@modules/diet-planner/utils/householdAccess';
import { useHousehold } from '@modules/household';
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
import { useFormat } from '@shared/hooks/useFormat';
import { cn } from '@shared/lib/utils';
import { useQueryClient } from '@tanstack/react-query';
import {
  ArrowDown,
  ArrowUp,
  MoreVertical,
  Package,
  Pencil,
  Plus,
  Search,
  Trash2,
} from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

interface Row {
  id: string;
  name: string;
  caloriesPer100g: number | null;
  proteinPer100g: number | null;
  carbsPer100g: number | null;
  fatPer100g: number | null;
  fiberPer100g?: number | null;
  densityGramsPerMl?: number | null;
  defaultUnit: string;
  visibility: string;
  canEdit: boolean;
}

function isIncomplete(p: Row): boolean {
  return (
    p.caloriesPer100g === null ||
    p.proteinPer100g === null ||
    p.carbsPer100g === null ||
    p.fatPer100g === null
  );
}

type Filter = 'all' | 'mine' | 'incomplete';

const COLUMN_LABEL: Record<ProductSortKey, string> = {
  name: 'name',
  calories: 'calories',
  protein: 'protein',
  carbs: 'carbs',
  fat: 'fat',
  fiber: 'fiber',
};

const SORT_KEYS: ProductSortKey[] = ['name', 'calories', 'protein', 'carbs', 'fat', 'fiber'];

export default function ProductList() {
  const { t } = useTranslation();
  const toast = useToast();
  const canCreate = canWriteLibrary(useHousehold().myRole);
  const {
    params,
    search,
    debouncedSearch,
    page,
    pageSize,
    setPage,
    setPageSize,
    setSearch,
    update,
  } = useListLocation();
  const fmt = useFormat();
  const filter: Filter =
    params.get('filter') === 'mine' || params.get('filter') === 'incomplete'
      ? (params.get('filter') as Filter)
      : 'all';
  const sortParam = params.get('sort') as ProductSortKey | null;
  const sortBy: ProductSortKey = sortParam && SORT_KEYS.includes(sortParam) ? sortParam : 'name';
  const sortDescending = params.get('dir') === 'desc';
  // Changing sort or filter always returns to page 1.
  const setSort = (key: ProductSortKey) => {
    const descending = key === sortBy && !sortDescending;
    update({
      sort: key === 'name' ? null : key,
      dir: descending ? 'desc' : null,
      page: null,
    });
  };
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const queryClient = useQueryClient();
  // Warm the detail cache on intent so the detail / edit page renders without suspending.
  // Best-effort: a failed prefetch is swallowed, the page itself surfaces the error.
  const prefetchProduct = (id: string) => {
    queryClient.query(productOptions(id)).catch(() => undefined);
  };

  const { data, isLoading, error } = useProducts({
    search: debouncedSearch,
    onlyMine: filter === 'mine',
    onlyIncomplete: filter === 'incomplete',
    sortBy,
    sortDescending,
    page,
    pageSize,
  });
  const deleteMutation = useDeleteProduct();

  const rows = (data?.items ?? []) as Row[];
  const hasFilters = !!search || filter !== 'all';

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
          <h1 className="text-26px font-bold">{t('products.title')}</h1>
          <p className="text-muted-foreground mt-1 text-sm">
            {t('products.subtitle_per100', { count: data?.totalCount ?? 0 })}
          </p>
        </div>
        {canCreate ? (
          <Button size="xl" asChild>
            <Link to="/diet-planner/products/new">
              <Plus className="size-4" />
              {t('products.add_product')}
            </Link>
          </Button>
        ) : null}
      </div>

      {/* Filter strip */}
      <div className="border-border bg-card rounded-18px flex flex-wrap items-center gap-2.5 border p-3.5">
        <div className="bg-secondary border-border focus-within:ring-primary h-38px min-w-180px rounded-12px flex flex-1 items-center gap-2 border px-3 focus-within:ring-2">
          <Search className="text-muted-foreground size-15px shrink-0" strokeWidth={2} />
          <input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
            }}
            placeholder={t('products.search_placeholder')}
            aria-label={t('products.search_placeholder')}
            className="text-foreground placeholder:text-muted-foreground text-13px w-full bg-transparent outline-none"
          />
        </div>

        <SegmentedControl
          className="ml-auto"
          label={t('products.filter_label')}
          value={filter}
          onChange={(value) => {
            update({ filter: value === 'all' ? null : value, page: null });
          }}
          options={[
            { value: 'all', label: t('products.filter_all') },
            { value: 'mine', label: t('products.only_mine') },
            { value: 'incomplete', label: t('products.filter_incomplete') },
          ]}
        />
      </div>

      {error ? <Banner variant="error">{error.message}</Banner> : null}

      {isLoading ? (
        <Skeleton className="h-420px rounded-22px w-full" />
      ) : rows.length === 0 ? (
        <EmptyState
          icon={Package}
          title={t('products.no_products_found')}
          description={hasFilters ? t('products.adjust_search') : t('products.start_creating')}
          action={
            hasFilters
              ? {
                  label: t('products.clear_filters'),
                  onClick: () => {
                    update({ search: null, filter: null, page: null });
                  },
                }
              : canCreate
                ? { label: t('products.add_first_product'), href: '/diet-planner/products/new' }
                : undefined
          }
        />
      ) : (
        <div className="border-border bg-card rounded-22px max-h-[70vh] overflow-auto border">
          <table className="text-13px w-full min-w-[720px] border-collapse">
            <thead className="bg-secondary text-muted-foreground sticky top-0 z-10">
              <tr>
                {SORT_KEYS.flatMap((key) => [
                  <th
                    key={key}
                    scope="col"
                    aria-sort={
                      key === sortBy ? (sortDescending ? 'descending' : 'ascending') : 'none'
                    }
                    className={cn(
                      'h-11 px-4 text-xs font-semibold uppercase',
                      key === 'name' ? 'text-left' : 'text-right',
                    )}
                  >
                    <button
                      type="button"
                      onClick={() => {
                        setSort(key);
                      }}
                      aria-label={t('products.sort_by', {
                        column: t(`products.table.${COLUMN_LABEL[key]}`),
                      })}
                      className="hover:text-foreground focus-visible:ring-primary inline-flex h-11 items-center gap-1 uppercase focus-visible:ring-2 focus-visible:outline-none"
                    >
                      {t(`products.table.${COLUMN_LABEL[key]}`)}
                      {key === sortBy ? (
                        sortDescending ? (
                          <ArrowDown className="size-3.5" aria-hidden />
                        ) : (
                          <ArrowUp className="size-3.5" aria-hidden />
                        )
                      ) : null}
                    </button>
                  </th>,
                  ...(key === 'name'
                    ? [
                        <th
                          key="unit"
                          scope="col"
                          className="h-11 px-4 text-left text-xs font-semibold uppercase"
                        >
                          {t('products.table.unit')}
                        </th>,
                      ]
                    : []),
                ])}
                <th className="w-12" aria-label={t('common.actions')} />
              </tr>
            </thead>
            <tbody>
              {rows.map((p) => (
                <tr
                  key={p.id}
                  className="border-border hover:bg-secondary focus-within:ring-primary relative h-11 border-t focus-within:ring-2 focus-within:ring-inset"
                >
                  <td className="px-4 py-2">
                    <span className="flex min-w-0 items-center gap-2">
                      <Link
                        to={`/diet-planner/products/${p.id}`}
                        className="truncate font-semibold after:absolute after:inset-0 focus:outline-none"
                        onMouseEnter={() => {
                          prefetchProduct(p.id);
                        }}
                        onFocus={() => {
                          prefetchProduct(p.id);
                        }}
                      >
                        {p.name}
                      </Link>
                      {isIncomplete(p) ? (
                        <IncompleteBadge label={t('products.incomplete_badge')} />
                      ) : null}
                      {p.visibility !== 'Household' ? (
                        <VisibilityBadge visibility={p.visibility} />
                      ) : null}
                    </span>
                  </td>
                  <td className="text-text-2 px-4">
                    <span
                      title={
                        p.defaultUnit === 'ml' && (p.densityGramsPerMl ?? null) === null
                          ? t('products.no_density')
                          : undefined
                      }
                    >
                      {unitLabel(p.defaultUnit, t)}
                      {p.defaultUnit === 'ml' && (p.densityGramsPerMl ?? null) === null ? (
                        <>
                          <span aria-hidden> *</span>
                          <span className="sr-only">{t('products.no_density')}</span>
                        </>
                      ) : null}
                    </span>
                  </td>
                  <td className="tnum px-4 text-right">{fmt.energy(p.caloriesPer100g)}</td>
                  <td className="tnum px-4 text-right">{fmt.grams(p.proteinPer100g)}</td>
                  <td className="tnum px-4 text-right">{fmt.grams(p.carbsPer100g)}</td>
                  <td className="tnum px-4 text-right">{fmt.grams(p.fatPer100g)}</td>
                  <td className="tnum px-4 text-right">{fmt.grams(p.fiberPer100g)}</td>
                  <td className="px-2">
                    <RowMenu
                      product={p}
                      onDelete={() => {
                        setDeleteId(p.id);
                      }}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {data ? (
        <Pagination
          page={page}
          pageSize={pageSize}
          totalCount={data.totalCount}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
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

function IncompleteBadge({ label }: { label: string }) {
  return (
    <span
      className="rounded-6px text-10px text-carbs px-1.5 py-0.5 font-bold"
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
          className="text-muted-foreground hover:text-foreground rounded-10px relative z-10 grid size-11 place-items-center"
        >
          <MoreVertical className="size-4" />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem asChild>
          <Link to={`/diet-planner/products/${product.id}`}>{t('common.view')}</Link>
        </DropdownMenuItem>
        {product.canEdit ? (
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
