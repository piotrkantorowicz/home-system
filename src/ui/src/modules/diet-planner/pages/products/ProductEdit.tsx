import { productOptions, useUpdateProduct } from '@modules/diet-planner/api/hooks/useProducts';
import { useToast } from '@shared/context/ToastContext';
import { useSuspenseQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';

import { ProductForm, type ProductFormData } from '../../components/products/ProductForm';

export default function ProductEdit() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const toast = useToast();
  const { data: product } = useSuspenseQuery(productOptions(id ?? ''));
  const updateMutation = useUpdateProduct(id ?? '');

  const handleSubmit = async (data: ProductFormData) => {
    try {
      await updateMutation.mutateAsync({
        name: data.name,
        calories: data.caloriesPer100g,
        protein: data.proteinPer100g,
        carbs: data.carbsPer100g,
        fat: data.fatPer100g,
        fiber: data.fiberPer100g ?? null,
        defaultUnit: data.defaultUnit,
        densityGramsPerMl: data.densityGramsPerMl ?? null,
        gramPerPiece: data.gramPerPiece ?? null,
      });
      toast.success(t('product_form.update_success'));
      void navigate(`/diet-planner/products/${id ?? ''}`);
    } catch {
      toast.error(t('product_form.update_error'));
    }
  };

  if (!product) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-destructive text-lg">{t('product_detail.not_found')}</div>
      </div>
    );
  }

  return (
    <div className="animate-fade-in-up mx-auto max-w-6xl px-4 py-6 md:px-8">
      <div className="mb-8">
        <h1 className="text-[26px] font-bold">{t('product_form.edit_title')}</h1>
        <p className="text-muted-foreground mt-1 text-sm">
          {t('product_form.update_subtitle', { name: product.name })}
        </p>
      </div>

      <ProductForm
        defaultValues={{
          name: product.name,
          caloriesPer100g: product.caloriesPer100g ?? 0,
          proteinPer100g: product.proteinPer100g ?? 0,
          carbsPer100g: product.carbsPer100g ?? 0,
          fatPer100g: product.fatPer100g ?? 0,
          fiberPer100g: product.fiberPer100g ?? undefined,
          defaultUnit: product.defaultUnit,
          densityGramsPerMl: product.densityGramsPerMl ?? undefined,
          gramPerPiece: product.gramPerPiece ?? undefined,
        }}
        onSubmit={handleSubmit}
        isSubmitting={updateMutation.isPending}
        submitLabel={t('product_form.update_btn')}
      />
    </div>
  );
}
