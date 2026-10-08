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
  PageContainer,
  Pagination,
  SegmentedControl,
  Skeleton,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { cn } from '@shared/lib/utils';
import { useQueryClient } from '@tanstack/react-query';
import { BookOpen, Plus, Search } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

type Filter = 'all' | 'mine' | 'high_protein' | 'quick';

const DOT = { protein: 'bg-protein', carbs: 'bg-carbs', fat: 'bg-fat' } as const;

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
  const raw = params.get('filter');
  const filter: Filter = raw === 'mine' || raw === 'high_protein' || raw === 'quick' ? raw : 'all';
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
    onlyMine: filter === 'mine',
    onlyHighProtein: filter === 'high_protein',
    onlyQuick: filter === 'quick',
    page,
    pageSize,
  });
  const deleteMutation = useDeleteRecipe();

  const items = (data?.items ?? []) as RecipeCardData[];
  const hasFilters = !!search || filter !== 'all';

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
    <PageContainer className="animate-fade-in flex flex-col gap-6">
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

        <SegmentedControl
          label={t('recipes.filter_label')}
          value={filter}
          onChange={setFilter}
          options={[
            { value: 'all', label: t('recipes.filter_all') },
            { value: 'mine', label: t('recipes.filter_mine') },
            { value: 'high_protein', label: t('recipes.filter_high_protein') },
            { value: 'quick', label: t('recipes.filter_quick') },
          ]}
        />
        <ul
          aria-label={t('recipes.legend_label')}
          className="text-muted-foreground text-12px ml-auto flex items-center gap-3"
        >
          {(['protein', 'carbs', 'fat'] as const).map((macro) => (
            <li key={macro} className="flex items-center gap-1.5">
              <span className={cn(DOT[macro], 'size-2 rounded-full')} aria-hidden />
              {t(`recipes.legend_${macro}`)}
            </li>
          ))}
        </ul>
      </div>

      {error ? <Banner variant="error">{error.message}</Banner> : null}

      {isLoading ? (
        <div className="gap-18px grid [grid-template-columns:repeat(auto-fill,minmax(250px,1fr))]">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="rounded-22px h-[170px]" />
          ))}
        </div>
      ) : items.length === 0 ? (
        <EmptyState
          icon={BookOpen}
          title={t('recipes.no_recipes_found')}
          description={hasFilters ? t('products.adjust_search') : t('recipes.start_creating')}
          action={
            hasFilters
              ? {
                  label: t('products.clear_filters'),
                  onClick: () => {
                    update({ search: null, filter: null, page: null });
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
          className="gap-18px grid [grid-template-columns:repeat(auto-fill,minmax(250px,1fr))]"
        >
          {items.map((recipe) => (
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
              className="border-border-strong text-muted-foreground hover:text-foreground hover:border-foreground/40 rounded-22px flex min-h-[170px] flex-col items-center justify-center gap-2 border border-dashed transition-colors"
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
    </PageContainer>
  );
}
