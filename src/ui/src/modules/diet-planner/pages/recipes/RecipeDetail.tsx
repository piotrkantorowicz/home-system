import { Link, useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Edit, Trash2, Clock, Users } from 'lucide-react';
import { useState } from 'react';
import { useRecipe, useDeleteRecipe } from '@modules/diet-planner/api/hooks/useRecipes';
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@shared/components/ui';
import { Badge } from '@shared/components/ui/Badge';
import { MacroDistributionCard } from '../../components/MacroDistributionCard';
import { unitLabel } from '@modules/diet-planner/unitLabel';

export default function RecipeDetail() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: recipe, isLoading, error } = useRecipe(id!);
  const deleteMutation = useDeleteRecipe();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  const handleDelete = async () => {
    if (id) {
      await deleteMutation.mutateAsync({ id });
      navigate('/diet-planner/recipes');
    }
  };

  if (isLoading) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-lg text-muted-foreground">{t('recipe_detail.loading')}</div>
      </div>
    );
  }

  if (error || !recipe) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-lg text-destructive">{t('recipe_detail.not_found')}</div>
      </div>
    );
  }

  return (
    <div className="p-8 lg:p-10 max-w-6xl mx-auto animate-fade-in-up">
      <Link to="/diet-planner/recipes">
        <Button variant="ghost" size="sm" className="mb-4 -ml-2">
          <ArrowLeft className="mr-2 h-4 w-4" />
          {t('recipe_detail.back')}
        </Button>
      </Link>

      <div className="mb-8 flex items-start justify-between">
        <div>
          <h1 className="text-4xl font-bold tracking-tight mb-3">{recipe.name}</h1>
          <div className="flex items-center gap-4 text-muted-foreground">
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
            <p className="mt-4 text-muted-foreground text-[0.95rem]">{recipe.description}</p>
          )}
        </div>

        {recipe.isOwner && (
          <div className="flex gap-2">
            <Link to={`/diet-planner/recipes/${id}/edit`}>
              <Button>
                <Edit className="mr-2 h-4 w-4" />
                {t('common.edit')}
              </Button>
            </Link>
            <Button variant="destructive" onClick={() => setDeleteDialogOpen(true)}>
              <Trash2 className="mr-2 h-4 w-4" />
              {t('common.delete')}
            </Button>
          </div>
        )}
      </div>

      {recipe.nutritionPerServing.calories != null && (
        <div className="grid gap-6 md:grid-cols-2 mb-6 stagger-children">
          <Card>
            <CardHeader>
              <CardTitle>{t('recipe_detail.nutrition_per_serving')}</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="border-b pb-3">
                  <div className="flex justify-between items-center">
                    <span className="font-semibold text-lg">
                      {t('recipe_detail.table.calories')}
                    </span>
                    <span className="text-3xl font-bold tracking-tight">
                      {Number(recipe.nutritionPerServing.calories).toFixed(0)}{' '}
                      <span className="text-lg text-muted-foreground font-normal">kcal</span>
                    </span>
                  </div>
                </div>

                <div className="space-y-3">
                  {recipe.nutritionPerServing.protein != null && (
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">
                        {t('recipe_detail.table.protein')}
                      </span>
                      <span className="font-medium">
                        {Number(recipe.nutritionPerServing.protein).toFixed(1)}g
                      </span>
                    </div>
                  )}
                  {recipe.nutritionPerServing.carbs != null && (
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">
                        {t('product_detail.carbohydrates')}
                      </span>
                      <span className="font-medium">
                        {Number(recipe.nutritionPerServing.carbs).toFixed(1)}g
                      </span>
                    </div>
                  )}
                  {recipe.nutritionPerServing.fat != null && (
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">{t('product_detail.fat')}</span>
                      <span className="font-medium">
                        {Number(recipe.nutritionPerServing.fat).toFixed(1)}g
                      </span>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('product_detail.fiber')}</span>
                    <span className="font-medium">
                      {Number(recipe.nutritionPerServing.fiber ?? 0).toFixed(1)}g
                    </span>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {recipe.totalNutrition.calories != null && (
            <Card>
              <CardHeader>
                <CardTitle>{t('recipe_detail.total_nutrition')}</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="border-b pb-3">
                    <div className="flex justify-between items-center">
                      <span className="font-semibold text-lg">
                        {t('recipe_detail.total_calories')}
                      </span>
                      <span className="text-3xl font-bold tracking-tight">
                        {Number(recipe.totalNutrition.calories).toFixed(0)}{' '}
                        <span className="text-lg text-muted-foreground font-normal">kcal</span>
                      </span>
                    </div>
                  </div>

                  <div className="space-y-3">
                    {recipe.totalNutrition.protein != null && (
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">
                          {t('recipe_detail.total_protein')}
                        </span>
                        <span className="font-medium">
                          {Number(recipe.totalNutrition.protein).toFixed(1)}g
                        </span>
                      </div>
                    )}
                    {recipe.totalNutrition.carbs != null && (
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">
                          {t('recipe_detail.total_carbs')}
                        </span>
                        <span className="font-medium">
                          {Number(recipe.totalNutrition.carbs).toFixed(1)}g
                        </span>
                      </div>
                    )}
                    {recipe.totalNutrition.fat != null && (
                      <div className="flex justify-between">
                        <span className="text-muted-foreground">
                          {t('recipe_detail.total_fat')}
                        </span>
                        <span className="font-medium">
                          {Number(recipe.totalNutrition.fat).toFixed(1)}g
                        </span>
                      </div>
                    )}
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">
                        {t('recipe_detail.total_fiber')}
                      </span>
                      <span className="font-medium">
                        {Number(recipe.totalNutrition.fiber ?? 0).toFixed(1)}g
                      </span>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      )}

      {recipe.nutritionPerServing.protein != null &&
        recipe.nutritionPerServing.carbs != null &&
        recipe.nutritionPerServing.fat != null && (
          <MacroDistributionCard
            protein={Number(recipe.nutritionPerServing.protein)}
            carbs={Number(recipe.nutritionPerServing.carbs)}
            fat={Number(recipe.nutritionPerServing.fat)}
            fiber={Number(recipe.nutritionPerServing.fiber ?? 0)}
            t={t}
            title={t('recipe_detail.macro_distribution')}
            className="mb-6"
          />
        )}

      <Card className="mb-6">
        <CardHeader>
          <CardTitle>
            {t('recipe_detail.ingredients', { count: recipe.ingredients.length })}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="rounded-xl border overflow-hidden">
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

      {recipe.instructions && (
        <Card>
          <CardHeader>
            <CardTitle>{t('recipe_detail.instructions')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="prose prose-sm max-w-none">
              <pre className="whitespace-pre-wrap font-sans text-[0.9rem] leading-relaxed">
                {recipe.instructions}
              </pre>
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
