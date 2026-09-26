import {
  recipeOptions,
  useDeleteRecipe,
  useRecipes,
} from '@modules/diet-planner/api/hooks/useRecipes';
import {
  RecipeCard,
  type RecipeCardData,
} from '@modules/diet-planner/components/recipes/RecipeCard';
import { useListLocation } from '@modules/diet-planner/hooks/useListLocation';
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
  EmptyState,
  Pagination,
  SegmentedControl,
  Skeleton,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useQueryClient } from '@tanstack/react-query';
import { BookOpen, Plus, Search } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

type Filter = 'all' | 'high_protein' | 'quick';

const n = (v: number | string | null | undefined): number =>
  typeof v === 'number' ? v : Number(v ?? 0);

export default function RecipeList() {
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
  const onlyMine = params.get('mine') === 'true';
  const filter: Filter =
    params.get('filter') === 'high_protein'
      ? 'high_protein'
      : params.get('filter') === 'quick'
        ? 'quick'
        : 'all';
  const setFilter = (filter: Filter) => {
    update({ filter: filter === 'all' ? null : filter, page: null });
  };
  const [toDelete, setToDelete] = useState<{ id: string; name: string } | null>(null);
  const queryClient = useQueryClient();
  // Warm the detail cache on intent so the detail / edit page renders without suspending.
  // Best-effort: a failed prefetch is swallowed, the page itself surfaces the error.
  const prefetchRecipe = (id: string) => {
    queryClient.query(recipeOptions(id)).catch(() => undefined);
  };

  const { data, isLoading, error } = useRecipes({
    search: debouncedSearch,
    onlyMine,
    page,
    pageSize,
  });
  const deleteMutation = useDeleteRecipe();

  const items = (data?.items ?? []) as RecipeCardData[];
  const filtered =
    filter === 'high_protein'
      ? items.filter((recipe) => n(recipe.nutritionPerServing?.protein) >= 20)
      : filter === 'quick'
        ? items.filter((recipe) => n(recipe.prepTimeMinutes) > 0 && n(recipe.prepTimeMinutes) <= 20)
        : items;
  const hasFilters = !!search || onlyMine || filter !== 'all';

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
          <h1 className="text-26px font-bold">{t('recipes.title')}</h1>
          <p className="text-muted-foreground mt-1 text-sm">{t('recipes.subtitle')}</p>
        </div>
        {canCreate ? (
          <Button size="xl" asChild>
            <Link to="/diet-planner/recipes/new">
              <Plus className="size-4" />
              {t('recipes.create_recipe')}
            </Link>
          </Button>
        ) : null}
      </div>

      <div className="border-border bg-card rounded-18px flex flex-wrap items-center gap-2.5 border p-3.5">
        <div className="bg-secondary border-border focus-within:ring-primary h-38px min-w-180px rounded-12px flex flex-1 items-center gap-2 border px-3 focus-within:ring-2">
          <Search className="text-muted-foreground size-15px shrink-0" strokeWidth={2} />
          <input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
            }}
            placeholder={t('recipes.search_placeholder')}
            aria-label={t('recipes.search_placeholder')}
            className="text-foreground placeholder:text-muted-foreground text-13px w-full bg-transparent outline-none"
          />
        </div>

        <button
          type="button"
          aria-pressed={onlyMine}
          onClick={() => {
            update({ mine: onlyMine ? null : 'true', page: null });
          }}
          className={
            onlyMine
              ? 'bg-accent text-accent-foreground text-12px inline-flex h-8 items-center gap-1.5 rounded-full px-3 font-semibold'
              : 'bg-secondary border-border text-text-2 hover:text-foreground text-12px inline-flex h-8 items-center gap-1.5 rounded-full border px-3 font-semibold'
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
        <div className="gap-18px grid [grid-template-columns:repeat(auto-fill,minmax(268px,1fr))]">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="rounded-22px h-[264px]" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={BookOpen}
          title={t('recipes.no_recipes_found')}
          description={hasFilters ? t('products.adjust_search') : t('recipes.start_creating')}
          action={
            hasFilters
              ? {
                  label: t('products.clear_filters'),
                  onClick: () => {
                    update({ search: null, mine: null, filter: null, page: null });
                  },
                }
              : canCreate
                ? { label: t('recipes.create_first_recipe'), href: '/diet-planner/recipes/new' }
                : undefined
          }
        />
      ) : (
        <div
          role="list"
          className="gap-18px grid [grid-template-columns:repeat(auto-fill,minmax(268px,1fr))]"
        >
          {filtered.map((recipe) => (
            <RecipeCard
              key={recipe.id}
              recipe={recipe}
              onPrefetch={() => {
                prefetchRecipe(recipe.id);
              }}
              onDelete={() => {
                setToDelete({ id: recipe.id, name: recipe.name });
              }}
            />
          ))}
          {canCreate ? (
            <Link
              to="/diet-planner/recipes/new"
              className="border-border-strong text-muted-foreground hover:text-foreground hover:border-foreground/40 rounded-22px flex min-h-[200px] flex-col items-center justify-center gap-2 border border-dashed transition-colors"
            >
              <span className="bg-accent text-accent-foreground grid size-11 place-items-center rounded-2xl">
                <Plus className="size-5" />
              </span>
              <span className="text-13px font-semibold">{t('recipes.create_tile')}</span>
            </Link>
          ) : null}
        </div>
      )}

      {data ? (
        <Pagination
          page={page}
          pageSize={pageSize}
          totalCount={n(data.totalCount)}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
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
