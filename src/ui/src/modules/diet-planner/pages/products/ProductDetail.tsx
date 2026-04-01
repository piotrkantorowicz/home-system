import { useProduct, useDeleteProduct } from '@modules/diet-planner/api/hooks/useProducts';
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
} from '@shared/components/ui';
import { Badge } from '@shared/components/ui/Badge';
import { ArrowLeft, Edit, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams, useNavigate } from 'react-router-dom';

import { MacroDistributionCard } from '../../components/MacroDistributionCard';

export default function ProductDetail() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: product, isLoading, error } = useProduct(id ?? '');
  const deleteMutation = useDeleteProduct();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  const handleDelete = async () => {
    if (id) {
      await deleteMutation.mutateAsync({ id });
      void navigate('/diet-planner/products');
    }
  };

  if (isLoading) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-muted-foreground text-lg">{t('product_detail.loading')}</div>
      </div>
    );
  }

  if (error || !product) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-destructive text-lg">{t('product_detail.not_found')}</div>
      </div>
    );
  }

  return (
    <div className="animate-fade-in-up mx-auto max-w-4xl p-8 lg:p-10">
      <div className="mb-8">
        <Button asChild variant="ghost" size="sm" className="mb-4 -ml-2">
          <Link to="/diet-planner/products">
            <ArrowLeft className="mr-2 h-4 w-4" />
            {t('product_detail.back')}
          </Link>
        </Button>

        <div className="flex items-start justify-between">
          <div>
            <h1 className="mb-3 text-4xl font-bold tracking-tight">{product.name}</h1>
            <div className="flex items-center gap-2">
              <Badge variant="secondary">{unitLabel(product.defaultUnit, t)}</Badge>
              {product.isOwner ? (
                <Badge variant="default">{t('common.you')}</Badge>
              ) : (
                <Badge variant="outline">{t('common.shared')}</Badge>
              )}
            </div>
          </div>

          {product.isOwner && (
            <div className="flex gap-2">
              <Button asChild>
                <Link to={`/diet-planner/products/${id ?? ''}/edit`}>
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
      </div>

      <div className="stagger-children grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('product_detail.nutrition_facts')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="border-b pb-3">
                <div className="flex items-center justify-between">
                  <span className="text-lg font-semibold">{t('products.table.calories')}</span>
                  <span className="text-3xl font-bold tracking-tight">
                    {(product.caloriesPer100g ?? 0).toFixed(1)}{' '}
                    <span className="text-muted-foreground text-lg font-normal">kcal</span>
                  </span>
                </div>
              </div>

              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('products.table.protein')}</span>
                  <span className="font-medium">{(product.proteinPer100g ?? 0).toFixed(1)}g</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('product_detail.carbohydrates')}</span>
                  <span className="font-medium">{(product.carbsPer100g ?? 0).toFixed(1)}g</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('product_detail.fat')}</span>
                  <span className="font-medium">{(product.fatPer100g ?? 0).toFixed(1)}g</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">{t('product_detail.fiber')}</span>
                  <span className="font-medium">{(product.fiberPer100g ?? 0).toFixed(1)}g</span>
                </div>
              </div>

              <div className="border-t pt-3">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">{t('product_detail.total_macros')}</span>
                  <span className="font-medium">
                    {(
                      (product.proteinPer100g ?? 0) +
                      (product.carbsPer100g ?? 0) +
                      (product.fatPer100g ?? 0)
                    ).toFixed(1)}
                    g
                  </span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('product_detail.conversions')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <h4 className="mb-1.5 font-medium">{t('product_detail.default_unit')}</h4>
              <p className="text-muted-foreground">{unitLabel(product.defaultUnit, t)}</p>
            </div>

            {product.densityGramsPerMl && (
              <div>
                <h4 className="mb-1.5 font-medium">{t('product_detail.density')}</h4>
                <p className="text-muted-foreground">{product.densityGramsPerMl.toFixed(2)} g/ml</p>
                <p className="text-muted-foreground mt-1 text-xs">
                  {t('product_detail.density_info', {
                    value: product.densityGramsPerMl.toFixed(2),
                  })}
                </p>
              </div>
            )}

            {product.gramPerPiece && (
              <div>
                <h4 className="mb-1.5 font-medium">{t('product_detail.weight_per_piece')}</h4>
                <p className="text-muted-foreground">{product.gramPerPiece.toFixed(1)}g</p>
                <p className="text-muted-foreground mt-1 text-xs">
                  {t('product_detail.piece_info', { value: product.gramPerPiece.toFixed(1) })}
                </p>
              </div>
            )}

            {!product.densityGramsPerMl && !product.gramPerPiece && (
              <p className="text-muted-foreground">{t('product_detail.no_conversion')}</p>
            )}
          </CardContent>
        </Card>
      </div>

      <MacroDistributionCard
        protein={product.proteinPer100g ?? 0}
        carbs={product.carbsPer100g ?? 0}
        fat={product.fatPer100g ?? 0}
        fiber={product.fiberPer100g ?? 0}
        t={t}
        className="mt-6"
      />

      <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('products.delete_dialog.title')}</DialogTitle>
            <DialogDescription>{t('products.delete_dialog.description')}</DialogDescription>
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
                ? t('products.delete_dialog.deleting')
                : t('products.delete_dialog.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
