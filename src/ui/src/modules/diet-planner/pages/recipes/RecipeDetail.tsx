import { useRecipe, useDeleteRecipe } from '@modules/diet-planner/api/hooks/useRecipes';
import { unitLabel } from '@modules/diet-planner/unitLabel';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@shared/components/ui';
import { Badge } from '@shared/components/ui/Badge';
import { ArrowLeft, Edit, Trash2, Clock, Users } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams, useNavigate } from 'react-router-dom';

export default function RecipeDetail() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: recipe, isLoading, error } = useRecipe(id ?? '');
  const deleteMutation = useDeleteRecipe();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  const handleDelete = async () => {
    if (id) {
      await deleteMutation.mutateAsync({ id });
      void navigate('/diet-planner/recipes');
    }
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-6xl p-8 lg:p-10">
        <Skeleton className="mb-4 h-8 w-24" />
        <div className="mb-8 flex items-start justify-between">
          <div className="space-y-3">
            <Skeleton className="h-10 w-72" />
            <div className="flex gap-4">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-6 w-14 rounded-full" />
            </div>
            <Skeleton className="h-4 w-96" />
          </div>
          <div className="flex gap-2">
            <Skeleton className="h-9 w-20 rounded-md" />
            <Skeleton className="h-9 w-20 rounded-md" />
          </div>
        </div>
        <div className="mb-6 grid gap-6 lg:grid-cols-3">
          <Card className="lg:col-span-2">
            <CardHeader>
              <Skeleton className="h-5 w-28" />
            </CardHeader>
            <CardContent>
              <div className="overflow-hidden rounded-xl border">
                <Table>
                  <TableHeader>
                    <TableRow className="bg-muted/30">
                      <TableHead><Skeleton className="h-4 w-20" /></TableHead>
                      <TableHead><Skeleton className="h-4 w-16" /></TableHead>
                      <TableHead><Skeleton className="h-4 w-12" /></TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {Array.from({ length: 5 }).map((_, i) => (
                      <TableRow key={i}>
                        <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                        <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                        <TableCell><Skeleton className="h-6 w-14 rounded-full" /></TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <Skeleton className="h-4 w-36" />
            </CardHeader>
            <CardContent className="space-y-4">
              <Skeleton className="h-10 w-24" />
              <div className="space-y-2.5">
                {Array.from({ length: 4 }).map((_, i) => (
                  <div key={i} className="flex justify-between">
                    <Skeleton className="h-4 w-16" />
                    <Skeleton className="h-4 w-10" />
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  if (error || !recipe) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-destructive text-lg">{t('recipe_detail.not_found')}</div>
      </div>
    );
  }

  return (
    <div className="animate-fade-in-up mx-auto max-w-6xl p-8 lg:p-10">
      <Button asChild variant="ghost" size="sm" className="mb-4 -ml-2">
        <Link to="/diet-planner/recipes">
          <ArrowLeft className="mr-2 h-4 w-4" />
          {t('recipe_detail.back')}
        </Link>
      </Button>

      <div className="mb-8 flex items-start justify-between">
        <div>
          <h1 className="mb-3 text-4xl font-bold tracking-tight">{recipe.name}</h1>
          <div className="text-muted-foreground flex items-center gap-4">
            <div className="flex items-center gap-2">
              <Users className="h-4 w-4" />
              <span>{t('recipes.servings', { count: Number(recipe.servings) })}</span>
            </div>
            {recipe.prepTimeMinutes && (
              <div className="flex items-center gap-2">
                <Clock className="h-4 w-4" />
                <span>
                  {recipe.prepTimeMinutes} {t('recipes.prep_time')}
                </span>
              </div>
            )}
            {recipe.isOwner ? (
              <Badge variant="default">{t('common.you')}</Badge>
            ) : (
              <Badge variant="outline">{t('common.shared')}</Badge>
            )}
          </div>
          {recipe.description && (
            <p className="text-muted-foreground mt-4 text-[0.95rem]">{recipe.description}</p>
          )}
        </div>

        {recipe.isOwner && (
          <div className="flex gap-2">
            <Button asChild>
              <Link to={`/diet-planner/recipes/${id ?? ''}/edit`}>
                <Edit className="mr-2 h-4 w-4" />
                {t('common.edit')}
              </Link>
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                setDeleteDialogOpen(true);
              }}
            >
              <Trash2 className="mr-2 h-4 w-4" />
              {t('common.delete')}
            </Button>
          </div>
        )}
      </div>

      <div className="stagger-children mb-6 grid gap-6 lg:grid-cols-3">
        {/* ── Ingredients (primary) ─────────────────────────────────── */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>{t('recipe_detail.ingredients')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="overflow-hidden rounded-xl border">
              <Table>
                <TableHeader>
                  <TableRow className="bg-muted/30">
                    <TableHead>{t('recipe_detail.table.product')}</TableHead>
                    <TableHead>{t('recipe_detail.table.amount')}</TableHead>
                    <TableHead>{t('recipe_detail.table.unit')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {recipe.ingredients.map((ingredient) => (
                    <TableRow key={ingredient.id}>
                      <TableCell className="font-medium">{ingredient.productName}</TableCell>
                      <TableCell>{Number(ingredient.amount).toFixed(1)}</TableCell>
                      <TableCell>
                        <Badge variant="secondary">{unitLabel(ingredient.unit, t)}</Badge>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>

        {/* ── Nutrition per serving (compact sidebar) ──────────────── */}
        {recipe.nutritionPerServing && (
          <Card>
            <CardHeader>
              <CardTitle className="text-base">
                {t('recipe_detail.nutrition_per_serving')}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="mb-4 border-b pb-3">
                <span className="text-3xl font-bold tracking-tight">
                  {Number(recipe.nutritionPerServing.calories).toFixed(0)}
                </span>
                <span className="text-muted-foreground ml-1 text-sm">kcal</span>
              </div>
              <div className="space-y-2.5 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('recipe_detail.table.protein')}</span>
                  <span className="font-medium">
                    {Number(recipe.nutritionPerServing.protein).toFixed(1)}g
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('product_detail.carbohydrates')}</span>
                  <span className="font-medium">
                    {Number(recipe.nutritionPerServing.carbs).toFixed(1)}g
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('product_detail.fat')}</span>
                  <span className="font-medium">
                    {Number(recipe.nutritionPerServing.fat).toFixed(1)}g
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('product_detail.fiber')}</span>
                  <span className="font-medium">
                    {Number(recipe.nutritionPerServing.fiber).toFixed(1)}g
                  </span>
                </div>
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      {/* ── Instructions (primary) ──────────────────────────────────── */}
      {recipe.instructions && (
        <Card className="mb-6">
          <CardHeader>
            <CardTitle>{t('recipe_detail.instructions')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="prose prose-sm max-w-none">
              <p className="text-[0.95rem] leading-7 whitespace-pre-wrap">{recipe.instructions}</p>
            </div>
          </CardContent>
        </Card>
      )}

      <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('recipes.delete_dialog.title')}</DialogTitle>
            <DialogDescription>
              {t('recipes.delete_dialog.description', { name: recipe.name })}
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
