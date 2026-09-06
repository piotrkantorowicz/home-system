import { useRecipes, useDeleteRecipe } from '@modules/diet-planner/api/hooks/useRecipes';
import {
  RecipeCard,
  type RecipeCardData,
} from '@modules/diet-planner/components/recipes/RecipeCard';
import {
  Banner,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  EmptyState,
  Pagination,
  SegmentedControl,
  Skeleton,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { BookOpen, Plus, Search } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

type Filter = 'all' | 'high_protein' | 'quick';

const n = (v: number | string | null | undefined): number =>
  typeof v === 'number' ? v : Number(v ?? 0);

export default function RecipeList() {
  const { t } = useTranslation();
  const toast = useToast();
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [onlyMine, setOnlyMine] = useState(false);
  const [filter, setFilter] = useState<Filter>('all');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [toDelete, setToDelete] = useState<{ id: string; name: string } | null>(null);

  const { data, isLoading, error } = useRecipes({
    search: debouncedSearch,
    onlyMine,
    page,
    pageSize,
  });
  const deleteMutation = useDeleteRecipe();

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

  const items = useMemo(() => (data?.items ?? []) as RecipeCardData[], [data]);
  const filtered = useMemo(() => {
    if (filter === 'high_protein') {
      return items.filter((r) => n(r.nutritionPerServing?.protein) >= 20);
    }
    if (filter === 'quick') {
      return items.filter((r) => n(r.prepTimeMinutes) > 0 && n(r.prepTimeMinutes) <= 20);
    }
    return items;
  }, [items, filter]);

  const handleDelete = async () => {
    if (!toDelete) return;
    try {
      await deleteMutation.mutateAsync({ id: toDelete.id });
      setToDelete(null);
    } catch {
      toast.error(t('common.error'));
    }
  };

  return (
    <div className="animate-fade-in flex flex-col gap-6 px-4 py-6 md:px-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[26px] font-bold">{t('recipes.title')}</h1>
          <p className="text-muted-foreground mt-1 text-sm">{t('recipes.subtitle')}</p>
        </div>
        <Button size="xl" asChild>
          <Link to="/diet-planner/recipes/new">
            <Plus className="size-4" />
            {t('recipes.create_recipe')}
          </Link>
        </Button>
      </div>

      <div className="border-border bg-card flex flex-wrap items-center gap-2.5 rounded-[18px] border p-3.5">
        <div className="bg-secondary border-border focus-within:ring-primary flex h-[38px] min-w-[180px] flex-1 items-center gap-2 rounded-[12px] border px-3 focus-within:ring-2">
          <Search className="text-muted-foreground size-[15px] shrink-0" strokeWidth={2} />
          <input
            value={search}
            onChange={(e) => {
              handleSearchChange(e.target.value);
            }}
            placeholder={t('recipes.search_placeholder')}
            aria-label={t('recipes.search_placeholder')}
            className="text-foreground placeholder:text-muted-foreground w-full bg-transparent text-[13px] outline-none"
          />
        </div>

        <button
          type="button"
          onClick={() => {
            setOnlyMine((v) => !v);
            setPage(1);
          }}
          className={
            onlyMine
              ? 'bg-accent text-accent-foreground inline-flex h-8 items-center gap-1.5 rounded-full px-3 text-[12px] font-semibold'
              : 'bg-secondary border-border text-text-2 hover:text-foreground inline-flex h-8 items-center gap-1.5 rounded-full border px-3 text-[12px] font-semibold'
          }
        >
          {t('recipes.my_recipes')}
          {onlyMine ? <span aria-hidden>×</span> : null}
        </button>

        <SegmentedControl
          className="ml-auto"
          label={t('recipes.filter_label')}
          value={filter}
          onChange={setFilter}
          options={[
            { value: 'all', label: t('recipes.filter_all') },
            { value: 'high_protein', label: t('recipes.filter_high_protein') },
            { value: 'quick', label: t('recipes.filter_quick') },
          ]}
        />
      </div>

      {error ? <Banner variant="error">{error.message}</Banner> : null}

      {isLoading ? (
        <div className="grid [grid-template-columns:repeat(auto-fill,minmax(268px,1fr))] gap-[18px]">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-[264px] rounded-[22px]" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={BookOpen}
          title={t('recipes.no_recipes_found')}
          description={debouncedSearch ? t('products.adjust_search') : t('recipes.start_creating')}
          action={
            debouncedSearch
              ? undefined
              : { label: t('recipes.create_first_recipe'), href: '/diet-planner/recipes/new' }
          }
        />
      ) : (
        <div
          role="list"
          className="grid [grid-template-columns:repeat(auto-fill,minmax(268px,1fr))] gap-[18px]"
        >
          {filtered.map((recipe) => (
            <RecipeCard
              key={recipe.id}
              recipe={recipe}
              onDelete={() => {
                setToDelete({ id: recipe.id, name: recipe.name });
              }}
            />
          ))}
          <Link
            to="/diet-planner/recipes/new"
            className="border-border-strong text-muted-foreground hover:text-foreground hover:border-foreground/40 flex min-h-[200px] flex-col items-center justify-center gap-2 rounded-[22px] border border-dashed transition-colors"
          >
            <span className="bg-accent text-accent-foreground grid size-11 place-items-center rounded-2xl">
              <Plus className="size-5" />
            </span>
            <span className="text-[13px] font-semibold">{t('recipes.create_tile')}</span>
          </Link>
        </div>
      )}

      {data ? (
        <Pagination
          page={page}
          pageSize={pageSize}
          totalCount={n(data.totalCount)}
          onPageChange={setPage}
          onPageSizeChange={(size) => {
            setPageSize(size);
            setPage(1);
          }}
        />
      ) : null}

      <Dialog
        open={toDelete !== null}
        onOpenChange={(open) => {
          if (!open) setToDelete(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('recipes.delete_dialog.title')}</DialogTitle>
            <DialogDescription>
              {t('recipes.delete_dialog.description', { name: toDelete?.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setToDelete(null);
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
                ? t('recipes.delete_dialog.deleting')
                : t('recipes.delete_dialog.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
