import { useProduct, useDeleteProduct } from '@modules/diet-planner/api/hooks/useProducts';
import { unitLabel } from '@modules/diet-planner/unitLabel';
import {
  Button,
  Card,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Skeleton,
  StatusPill,
} from '@shared/components/ui';
import { formatNumber } from '@shared/lib/utils';
import { ChevronRight, Pencil, Trash2 } from 'lucide-react';
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
      <div className="mx-auto max-w-5xl px-4 py-6 md:px-8">
        <Skeleton className="mb-4 h-4 w-40" />
        <Skeleton className="mb-6 h-9 w-64" />
        <div className="grid gap-[18px] lg:grid-cols-3">
          <Skeleton className="h-72 rounded-[22px] lg:col-span-2" />
          <Skeleton className="h-72 rounded-[22px]" />
        </div>
        <Skeleton className="mt-[18px] h-48 w-full rounded-[22px]" />
      </div>
    );
  }

  if (error || !product) {
    return (
      <div className="px-4 py-6 md:px-8">
        <p className="text-destructive">{t('product_detail.not_found')}</p>
      </div>
    );
  }

  const macroTotal =
    (product.proteinPer100g ?? 0) + (product.carbsPer100g ?? 0) + (product.fatPer100g ?? 0);

  const rows: { label: string; grams: number; token: string }[] = [
    { label: t('products.table.protein'), grams: product.proteinPer100g ?? 0, token: 'protein' },
    { label: t('product_detail.carbohydrates'), grams: product.carbsPer100g ?? 0, token: 'carbs' },
    { label: t('product_detail.fat'), grams: product.fatPer100g ?? 0, token: 'fat' },
    { label: t('product_detail.fiber'), grams: product.fiberPer100g ?? 0, token: 'fiber' },
  ];

  return (
    <div className="animate-fade-in mx-auto flex max-w-5xl flex-col gap-5 px-4 py-6 md:px-8">
      <nav className="text-muted-foreground flex items-center gap-1 text-[12.5px]">
        <Link to="/diet-planner/products" className="hover:text-foreground">
          {t('products.title')}
        </Link>
        <ChevronRight className="size-3.5" />
        <span className="text-text-2 font-semibold">{product.name}</span>
      </nav>

      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[26px] font-bold tracking-tight">{product.name}</h1>
          <div className="mt-2 flex items-center gap-2">
            <StatusPill variant="neutral">{unitLabel(product.defaultUnit, t)}</StatusPill>
            <StatusPill variant={product.isOwner ? 'good' : 'neutral'}>
              {product.isOwner ? t('common.you') : t('common.shared')}
            </StatusPill>
          </div>
        </div>

        {product.isOwner && (
          <div className="flex flex-wrap gap-2.5">
            <Button size="xl" asChild>
              <Link to={`/diet-planner/products/${id ?? ''}/edit`}>
                <Pencil className="size-4" />
                {t('common.edit')}
              </Link>
            </Button>
            <Button
              size="xl"
              variant="outline"
              onClick={() => {
                setDeleteDialogOpen(true);
              }}
            >
              <Trash2 className="text-destructive size-4" />
            </Button>
          </div>
        )}
      </div>

      <div className="grid gap-[18px] lg:grid-cols-3">
        <Card className="flex flex-col gap-5 p-[22px] lg:col-span-2">
          <div className="text-[15px] font-bold">{t('product_detail.nutrition_facts')}</div>

          <div className="border-border flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1 border-b pb-4">
            <span className="text-[15px] font-semibold">{t('products.table.calories')}</span>
            <span className="numeral text-[34px] leading-none font-bold">
              <span className="tnum">{formatNumber(product.caloriesPer100g ?? 0)}</span>
              <span className="text-muted-foreground ml-1 text-[12px] font-medium">
                kcal / 100 {t('product_form.units.g')}
              </span>
            </span>
          </div>

          <div className="flex flex-col gap-2.5">
            {rows.map((r) => {
              const pct = macroTotal > 0 ? Math.min(100, (r.grams / macroTotal) * 100) : 0;
              return (
                <div key={r.label} className="flex flex-col gap-1">
                  <div className="flex justify-between text-[12.5px]">
                    <span className="font-semibold">{r.label}</span>
                    <span className="text-text-2 tnum">{r.grams.toFixed(1)} g</span>
                  </div>
                  <div className="bg-muted h-1.5 overflow-hidden rounded-full">
                    <div
                      className="h-full rounded-full"
                      style={{
                        width: `${String(pct)}%`,
                        background: `var(--color-${r.token})`,
                      }}
                    />
                  </div>
                </div>
              );
            })}
          </div>

          <div className="border-border flex justify-between border-t pt-3 text-[12.5px]">
            <span className="text-muted-foreground">{t('product_detail.total_macros')}</span>
            <span className="tnum font-medium">{macroTotal.toFixed(1)} g</span>
          </div>
        </Card>

        <Card className="flex flex-col gap-4 p-[22px]">
          <div className="text-[15px] font-bold">{t('product_detail.conversions')}</div>

          <div>
            <div className="text-muted-foreground text-[11px] font-semibold uppercase">
              {t('product_detail.default_unit')}
            </div>
            <p className="mt-0.5 text-[13px]">{unitLabel(product.defaultUnit, t)}</p>
          </div>

          {product.densityGramsPerMl ? (
            <div>
              <div className="text-muted-foreground text-[11px] font-semibold uppercase">
                {t('product_detail.density')}
              </div>
              <p className="tnum mt-0.5 text-[13px]">{product.densityGramsPerMl.toFixed(2)} g/ml</p>
              <p className="text-muted-foreground mt-1 text-[11px]">
                {t('product_detail.density_info', { value: product.densityGramsPerMl.toFixed(2) })}
              </p>
            </div>
          ) : null}

          {product.gramPerPiece ? (
            <div>
              <div className="text-muted-foreground text-[11px] font-semibold uppercase">
                {t('product_detail.weight_per_piece')}
              </div>
              <p className="tnum mt-0.5 text-[13px]">{product.gramPerPiece.toFixed(1)} g</p>
              <p className="text-muted-foreground mt-1 text-[11px]">
                {t('product_detail.piece_info', { value: product.gramPerPiece.toFixed(1) })}
              </p>
            </div>
          ) : null}

          {!product.densityGramsPerMl && !product.gramPerPiece ? (
            <p className="text-muted-foreground text-[13px]">{t('product_detail.no_conversion')}</p>
          ) : null}
        </Card>
      </div>

      <MacroDistributionCard
        protein={product.proteinPer100g ?? 0}
        carbs={product.carbsPer100g ?? 0}
        fat={product.fatPer100g ?? 0}
        fiber={product.fiberPer100g ?? 0}
        t={t}
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
