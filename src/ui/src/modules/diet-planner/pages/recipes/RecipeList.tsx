import { useRecipes, useDeleteRecipe } from '@modules/diet-planner/api/hooks/useRecipes';
import {
  Button,
  Input,
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  Pagination,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@shared/components/ui';
import { Badge } from '@shared/components/ui/Badge';
import {
  Plus,
  Search,
  Edit,
  Trash2,
  Eye,
  Grid,
  List as ListIcon,
  Clock,
  Users,
} from 'lucide-react';
import { useState, useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

export default function RecipeList() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [onlyMine, setOnlyMine] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [recipeToDelete, setRecipeToDelete] = useState<{ id: string; name: string } | null>(null);

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

  const handleDelete = async () => {
    if (recipeToDelete) {
      await deleteMutation.mutateAsync({ id: recipeToDelete.id });
      setDeleteDialogOpen(false);
      setRecipeToDelete(null);
    }
  };

  const openDeleteDialog = (id: string, name: string) => {
    setRecipeToDelete({ id, name });
    setDeleteDialogOpen(true);
  };

  return (
    <div className="animate-fade-in-up p-8 lg:p-10">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="mb-2 text-4xl font-bold tracking-tight">{t('recipes.title')}</h1>
          <p className="text-muted-foreground text-[0.95rem]">{t('recipes.subtitle')}</p>
        </div>
        <Button asChild>
          <Link to="/diet-planner/recipes/new">
            <Plus className="mr-2 h-4 w-4" />
            {t('recipes.create_recipe')}
          </Link>
        </Button>
      </div>

      <div className="mb-6 flex items-center gap-4">
        <div className="relative max-w-sm flex-1">
          <Search className="text-muted-foreground absolute top-1/2 left-3.5 h-4 w-4 -translate-y-1/2" />
          <Input
            placeholder={t('recipes.search_placeholder')}
            value={search}
            onChange={(e) => {
              handleSearchChange(e.target.value);
            }}
            className="pl-10"
          />
        </div>
        <Button
          variant={onlyMine ? 'default' : 'outline'}
          onClick={() => {
            setOnlyMine(!onlyMine);
            setPage(1);
          }}
        >
          {onlyMine ? t('recipes.show_my_recipes') : t('recipes.show_only_mine')}
        </Button>
        <div className="flex gap-1 rounded-lg border p-1">
          <Button
            variant={viewMode === 'grid' ? 'secondary' : 'ghost'}
            size="icon"
            onClick={() => {
              setViewMode('grid');
            }}
          >
            <Grid className="h-4 w-4" />
          </Button>
          <Button
            variant={viewMode === 'list' ? 'secondary' : 'ghost'}
            size="icon"
            onClick={() => {
              setViewMode('list');
            }}
          >
            <ListIcon className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {error && (
        <div className="border-destructive/30 bg-destructive/5 text-destructive animate-scale-in mb-4 rounded-xl border p-4">
          {t('common.error')}: {error.message}
        </div>
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="text-muted-foreground text-lg">{t('common.loading')}</div>
        </div>
      ) : (
        <>
          {data && (
            <Pagination
              page={page}
              pageSize={pageSize}
              totalCount={Number(data.totalCount)}
              onPageChange={setPage}
              onPageSizeChange={(size) => {
                setPageSize(size);
                setPage(1);
              }}
            />
          )}
          {data?.items.length === 0 ? (
            <div className="animate-fade-in flex flex-col items-center justify-center py-16 text-center">
              <p className="mb-2 text-xl font-semibold">{t('recipes.no_recipes_found')}</p>
              <p className="text-muted-foreground mb-6">
                {debouncedSearch ? t('products.adjust_search') : t('recipes.start_creating')}
              </p>
              {!debouncedSearch && (
                <Button asChild>
                  <Link to="/diet-planner/recipes/new">
                    <Plus className="mr-2 h-4 w-4" />
                    {t('recipes.create_first_recipe')}
                  </Link>
                </Button>
              )}
            </div>
          ) : (
            <>
              {viewMode === 'grid' ? (
                <div className="stagger-children grid gap-5 md:grid-cols-2 lg:grid-cols-3">
                  {data?.items.map((recipe) => (
                    <Card
                      key={recipe.id}
                      className="group transition-all duration-300 hover:-translate-y-1 hover:shadow-lg"
                    >
                      <CardHeader>
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <CardTitle className="text-lg">{recipe.name}</CardTitle>
                            {recipe.description && (
                              <CardDescription className="mt-2 line-clamp-2">
                                {recipe.description}
                              </CardDescription>
                            )}
                          </div>
                          {recipe.isOwner ? (
                            <Badge variant="default">{t('common.you')}</Badge>
                          ) : (
                            <Badge variant="outline">{t('common.shared')}</Badge>
                          )}
                        </div>
                      </CardHeader>
                      <CardContent>
                        <div className="space-y-3">
                          <div className="text-muted-foreground flex items-center gap-4 text-sm">
                            <div className="flex items-center gap-1.5">
                              <Users className="h-4 w-4" />
                              <span>
                                {t('recipes.servings', { count: Number(recipe.servings) })}
                              </span>
                            </div>
                            {recipe.prepTimeMinutes && (
                              <div className="flex items-center gap-1.5">
                                <Clock className="h-4 w-4" />
                                <span>
                                  {recipe.prepTimeMinutes} {t('recipes.prep_time')}
                                </span>
                              </div>
                            )}
                          </div>

                          <div className="grid grid-cols-2 gap-2">
                            <div className="bg-muted/40 rounded-lg p-2.5">
                              <p className="text-muted-foreground text-xs">
                                {t('recipes.per_serving')}
                              </p>
                              <p className="mt-0.5 text-sm font-semibold">
                                {`${Number(recipe.nutritionPerServing?.calories ?? 0).toFixed(0)} kcal`}
                              </p>
                            </div>
                            <div className="bg-muted/40 rounded-lg p-2.5">
                              <p className="text-muted-foreground text-xs">
                                {t('products.table.protein')}
                              </p>
                              <p className="mt-0.5 text-sm font-semibold">
                                {`${Number(recipe.nutritionPerServing?.protein ?? 0).toFixed(1)}g`}
                              </p>
                            </div>
                          </div>

                          <div className="flex gap-2 border-t pt-3">
                            <Button asChild variant="outline" size="sm" className="w-full flex-1">
                              <Link to={`/diet-planner/recipes/${recipe.id}`}>
                                <Eye className="mr-2 h-4 w-4" />
                                {t('recipes.view')}
                              </Link>
                            </Button>
                            {recipe.isOwner && (
                              <>
                                <Button asChild variant="outline" size="sm" aria-label="Edit">
                                  <Link to={`/diet-planner/recipes/${recipe.id}/edit`}>
                                    <Edit className="h-4 w-4" />
                                  </Link>
                                </Button>
                                <Button
                                  variant="outline"
                                  size="sm"
                                  aria-label="Delete"
                                  onClick={() => {
                                    openDeleteDialog(recipe.id, recipe.name);
                                  }}
                                >
                                  <Trash2 className="text-destructive h-4 w-4" />
                                </Button>
                              </>
                            )}
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              ) : (
                <div className="stagger-children space-y-3">
                  {data?.items.map((recipe) => (
                    <Card key={recipe.id} className="transition-all duration-200 hover:shadow-md">
                      <CardContent className="p-5">
                        <div className="flex items-center justify-between">
                          <div className="flex-1">
                            <div className="mb-1.5 flex items-center gap-3">
                              <h3 className="text-lg font-semibold">{recipe.name}</h3>
                              {recipe.isOwner ? (
                                <Badge variant="default">{t('common.you')}</Badge>
                              ) : (
                                <Badge variant="outline">{t('common.shared')}</Badge>
                              )}
                            </div>
                            <div className="text-muted-foreground flex items-center gap-5 text-sm">
                              <span>
                                {t('recipes.servings', { count: Number(recipe.servings) })}
                              </span>
                              {recipe.prepTimeMinutes && (
                                <span>
                                  {recipe.prepTimeMinutes} {t('recipes.prep_time')}
                                </span>
                              )}
                              <span>
                                {Number(recipe.nutritionPerServing?.calories ?? 0).toFixed(0)} kcal/
                                {t('recipes.per_serving').toLowerCase()}
                              </span>
                              <span>
                                P: {Number(recipe.nutritionPerServing?.protein ?? 0).toFixed(1)}g
                              </span>
                              <span>
                                C: {Number(recipe.nutritionPerServing?.carbs ?? 0).toFixed(1)}g
                              </span>
                              <span>
                                F: {Number(recipe.nutritionPerServing?.fat ?? 0).toFixed(1)}g
                              </span>
                            </div>
                          </div>
                          <div className="flex gap-1">
                            <Button
                              asChild
                              variant="ghost"
                              size="icon"
                              className="hover:text-primary"
                              aria-label="View"
                            >
                              <Link to={`/diet-planner/recipes/${recipe.id}`}>
                                <Eye className="h-4 w-4" />
                              </Link>
                            </Button>
                            {recipe.isOwner && (
                              <>
                                <Button
                                  asChild
                                  variant="ghost"
                                  size="icon"
                                  className="hover:text-primary"
                                  aria-label="Edit"
                                >
                                  <Link to={`/diet-planner/recipes/${recipe.id}/edit`}>
                                    <Edit className="h-4 w-4" />
                                  </Link>
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="icon"
                                  aria-label="Delete"
                                  onClick={() => {
                                    openDeleteDialog(recipe.id, recipe.name);
                                  }}
                                  className="hover:text-destructive"
                                >
                                  <Trash2 className="h-4 w-4" />
                                </Button>
                              </>
                            )}
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              )}
            </>
          )}
        </>
      )}

      <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('recipes.delete_dialog.title')}</DialogTitle>
            <DialogDescription>
              {t('recipes.delete_dialog.description', { name: recipeToDelete?.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setDeleteDialogOpen(false);
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
