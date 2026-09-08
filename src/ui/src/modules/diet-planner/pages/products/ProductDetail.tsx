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
import { cn, formatNumber } from '@shared/lib/utils';
import { ChevronRight, Pencil, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams, useNavigate } from 'react-router-dom';

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

  const macros = [
    {
      key: 'protein',
      label: t('products.table.protein'),
      grams: product.proteinPer100g,
      colorClass: 'bg-protein',
    },
    {
      key: 'carbs',
      label: t('product_detail.carbohydrates'),
      grams: product.carbsPer100g,
      colorClass: 'bg-carbs',
    },
    {
      key: 'fat',
      label: t('product_detail.fat'),
      grams: product.fatPer100g,
      colorClass: 'bg-fat',
    },
  ] as const;
  const hasCompleteMacroData = macros.every((macro) => macro.grams !== null);
  const totalMacroGrams = macros.reduce((total, macro) => total + (macro.grams ?? 0), 0);
  const macroShare = (grams: number | null): number | null =>
    hasCompleteMacroData && grams !== null
      ? totalMacroGrams > 0
        ? Math.round((grams / totalMacroGrams) * 100)
        : 0
      : null;

  const rows = [
    ...macros.map((macro) => ({ ...macro, share: macroShare(macro.grams) })),
    {
      key: 'fiber',
      label: t('product_detail.fiber'),
      grams: product.fiberPer100g,
      colorClass: 'bg-fiber',
      share: null,
    },
  ] as const;

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
              aria-label={t('common.delete')}
              onClick={() => {
                setDeleteDialogOpen(true);
              }}
            >
              <Trash2 className="text-destructive size-4" />
            </Button>
          </div>
        )}
      </div>

      <div className="grid items-start gap-[18px] lg:grid-cols-3">
        <Card className="flex flex-col gap-5 p-[22px] lg:col-span-2">
          <div className="flex items-center justify-between gap-3">
            <h2 className="text-[15px] font-bold">{t('product_detail.nutrition_facts')}</h2>
            <span className="bg-secondary text-text-2 rounded-full px-2.5 py-1 text-[11px] font-semibold">
              {t('product_detail.per_100g')}
            </span>
          </div>

          <div className="border-border flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1 border-b pb-4">
            <span className="text-[15px] font-semibold">{t('products.table.calories')}</span>
            <span className="numeral text-[34px] leading-none font-bold">
              <span className="tnum">
                {product.caloriesPer100g === null ? '—' : formatNumber(product.caloriesPer100g)}
              </span>
              <span className="text-muted-foreground ml-1 text-[12px] font-medium">kcal</span>
            </span>
          </div>

          {hasCompleteMacroData ? (
            <div className="flex flex-col gap-2.5">
              <div className="flex items-baseline justify-between gap-3">
                <span className="text-[12.5px] font-semibold">
                  {t('product_detail.macro_balance')}
                </span>
                <span className="text-muted-foreground text-[11px]">
                  {t('product_detail.share_by_weight')}
                </span>
              </div>
              <div
                role="img"
                aria-label={t('product_detail.macro_balance_description', {
                  protein: macroShare(product.proteinPer100g),
                  carbs: macroShare(product.carbsPer100g),
                  fat: macroShare(product.fatPer100g),
                })}
                className="bg-muted flex h-3 overflow-hidden rounded-full"
              >
                {totalMacroGrams > 0
                  ? macros.map((macro) => (
                      <span
                        key={macro.key}
                        aria-hidden="true"
                        className={macro.colorClass}
                        style={{ flexGrow: macro.grams ?? 0 }}
                      />
                    ))
                  : null}
              </div>
            </div>
          ) : null}

          <dl className="divide-border divide-y">
            {rows.map((row) => (
              <div key={row.label} className="flex justify-between gap-4 py-3 text-sm">
                <dt className="inline-flex items-center gap-2 font-medium">
                  <span aria-hidden="true" className={cn('size-2 rounded-full', row.colorClass)} />
                  {row.label}
                </dt>
                <dd className="text-text-2 tnum text-right">
                  {row.grams === null || row.grams === undefined
                    ? '—'
                    : `${row.grams.toFixed(1)} g`}
                  {row.share !== null ? (
                    <span className="text-muted-foreground ml-2 text-[12px]">
                      ({row.share.toFixed(0)}%)
                    </span>
                  ) : null}
                </dd>
              </div>
            ))}
          </dl>

          {hasCompleteMacroData ? (
            <p className="text-muted-foreground text-[11px] leading-relaxed">
              {t('product_detail.macro_balance_note')}
            </p>
          ) : null}
        </Card>

        <Card className="flex flex-col gap-4 p-[22px]">
          <h2 className="text-[15px] font-bold">{t('product_detail.conversions')}</h2>

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
