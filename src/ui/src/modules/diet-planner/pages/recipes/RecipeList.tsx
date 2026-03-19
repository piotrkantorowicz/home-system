import { useState, useRef, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
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
  useEffect(() => () => clearTimeout(searchTimerRef.current), []);

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
    <div className="p-8 lg:p-10 animate-fade-in-up">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-4xl font-bold tracking-tight mb-2">{t('recipes.title')}</h1>
          <p className="text-muted-foreground text-[0.95rem]">{t('recipes.subtitle')}</p>
        </div>
        <Link to="/diet-planner/recipes/new">
          <Button>
            <Plus className="mr-2 h-4 w-4" />
            {t('recipes.create_recipe')}
          </Button>
        </Link>
      </div>

      <div className="mb-6 flex items-center gap-4">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder={t('recipes.search_placeholder')}
            value={search}
            onChange={(e) => handleSearchChange(e.target.value)}
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
        <div className="flex gap-1 border rounded-lg p-1">
          <Button
            variant={viewMode === 'grid' ? 'secondary' : 'ghost'}
            size="icon"
            onClick={() => setViewMode('grid')}
          >
            <Grid className="h-4 w-4" />
          </Button>
          <Button
            variant={viewMode === 'list' ? 'secondary' : 'ghost'}
            size="icon"
            onClick={() => setViewMode('list')}
          >
            <ListIcon className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {error && (
        <div className="mb-4 rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-destructive animate-scale-in">
          {t('common.error')}: {error.message}
        </div>
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="text-muted-foreground text-lg">{t('common.loading')}</div>
        </div>
      ) : data?.items?.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center animate-fade-in">
          <p className="text-xl font-semibold mb-2">{t('recipes.no_recipes_found')}</p>
          <p className="text-muted-foreground mb-6">
            {debouncedSearch ? t('products.adjust_search') : t('recipes.start_creating')}
          </p>
          {!debouncedSearch && (
            <Link to="/diet-planner/recipes/new">
              <Button>
                <Plus className="mr-2 h-4 w-4" />
                {t('recipes.create_first_recipe')}
              </Button>
            </Link>
          )}
        </div>
      ) : (
        <>
          {viewMode === 'grid' ? (
            <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3 stagger-children">
              {data?.items?.map((recipe) => (
                <Card
                  key={recipe.id}
                  className="group hover:shadow-lg hover:-translate-y-1 transition-all duration-300"
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
                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <div className="flex items-center gap-1.5">
                          <Users className="h-4 w-4" />
                          <span>{t('recipes.servings', { count: Number(recipe.servings) })}</span>
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
                        <div className="rounded-lg bg-muted/40 p-2.5">
                          <p className="text-xs text-muted-foreground">
                            {t('recipes.per_serving')}
                          </p>
                          <p className="font-semibold text-sm mt-0.5">
                            {recipe.nutritionPerServing.calories != null
                              ? `${Number(recipe.nutritionPerServing.calories).toFixed(0)} kcal`
                              : 'N/A'}
                          </p>
                        </div>
                        <div className="rounded-lg bg-muted/40 p-2.5">
                          <p className="text-xs text-muted-foreground">
                            {t('products.table.protein')}
                          </p>
                          <p className="font-semibold text-sm mt-0.5">
                            {recipe.nutritionPerServing.protein != null
                              ? `${Number(recipe.nutritionPerServing.protein).toFixed(1)}g`
                              : 'N/A'}
                          </p>
                        </div>
                      </div>

                      <div className="flex gap-2 pt-3 border-t">
                        <Link to={`/diet-planner/recipes/${recipe.id}`} className="flex-1">
                          <Button variant="outline" size="sm" className="w-full">
                            <Eye className="mr-2 h-4 w-4" />
                            {t('recipes.view')}
                          </Button>
                        </Link>
                        {recipe.isOwner && (
                          <>
                            <Link to={`/diet-planner/recipes/${recipe.id}/edit`} aria-label="Edit">
                              <Button variant="outline" size="sm">
                                <Edit className="h-4 w-4" />
                              </Button>
                            </Link>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => openDeleteDialog(recipe.id, recipe.name)}
                            >
                              <Trash2 className="h-4 w-4 text-destructive" />
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
            <div className="space-y-3 stagger-children">
              {data?.items?.map((recipe) => (
                <Card key={recipe.id} className="hover:shadow-md transition-all duration-200">
                  <CardContent className="p-5">
                    <div className="flex items-center justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-1.5">
                          <h3 className="text-lg font-semibold">{recipe.name}</h3>
                          {recipe.isOwner ? (
                            <Badge variant="default">{t('common.you')}</Badge>
                          ) : (
                            <Badge variant="outline">{t('common.shared')}</Badge>
                          )}
                        </div>
                        <div className="flex items-center gap-5 text-sm text-muted-foreground">
                          <span>{t('recipes.servings', { count: Number(recipe.servings) })}</span>
                          {recipe.prepTimeMinutes && (
                            <span>
                              {recipe.prepTimeMinutes} {t('recipes.prep_time')}
                            </span>
                          )}
                          {recipe.nutritionPerServing.calories != null && (
                            <span>
                              {Number(recipe.nutritionPerServing.calories).toFixed(0)} kcal/
                              {t('recipes.per_serving').toLowerCase()}
                            </span>
                          )}
                          {recipe.nutritionPerServing.protein != null && (
                            <span>P: {Number(recipe.nutritionPerServing.protein).toFixed(1)}g</span>
                          )}
                          {recipe.nutritionPerServing.carbs != null && (
                            <span>C: {Number(recipe.nutritionPerServing.carbs).toFixed(1)}g</span>
                          )}
                          {recipe.nutritionPerServing.fat != null && (
                            <span>F: {Number(recipe.nutritionPerServing.fat).toFixed(1)}g</span>
                          )}
                        </div>
                      </div>
                      <div className="flex gap-1">
                        <Link to={`/diet-planner/recipes/${recipe.id}`}>
                          <Button variant="ghost" size="icon" className="hover:text-primary">
                            <Eye className="h-4 w-4" />
                          </Button>
                        </Link>
                        {recipe.isOwner && (
                          <>
                            <Link to={`/diet-planner/recipes/${recipe.id}/edit`} aria-label="Edit">
                              <Button variant="ghost" size="icon" className="hover:text-primary">
                                <Edit className="h-4 w-4" />
                              </Button>
                            </Link>
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => openDeleteDialog(recipe.id, recipe.name)}
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
            <Button variant="outline" onClick={() => setDeleteDialogOpen(false)}>
              {t('common.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
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
