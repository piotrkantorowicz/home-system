import { useProduct, useUpdateProduct } from '@modules/diet-planner/api/hooks/useProducts';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';

import { ProductForm, type ProductFormData } from '../../components/products/ProductForm';

export default function ProductEdit() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: product, isLoading } = useProduct(id ?? '');
  const updateMutation = useUpdateProduct(id ?? '');

  const handleSubmit = async (data: ProductFormData) => {
    try {
      await updateMutation.mutateAsync({
        name: data.name,
        caloriesPer100g: data.caloriesPer100g,
        proteinPer100g: data.proteinPer100g,
        carbsPer100g: data.carbsPer100g,
        fatPer100g: data.fatPer100g,
        fiberPer100g: data.fiberPer100g ?? null,
        defaultUnit: data.defaultUnit,
        densityGramsPerMl: data.densityGramsPerMl ?? null,
        gramPerPiece: data.gramPerPiece ?? null,
      });
      void navigate(`/diet-planner/products/${id ?? ''}`);
    } catch (error) {
      console.error('Failed to update product:', error);
    }
  };

  if (isLoading) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-muted-foreground text-lg">{t('common.loading')}</div>
      </div>
    );
  }

  if (!product) {
    return (
      <div className="p-8 lg:p-10">
        <div className="text-destructive text-lg">{t('product_detail.not_found')}</div>
      </div>
    );
  }

  return (
    <div className="animate-fade-in-up mx-auto max-w-4xl p-8 lg:p-10">
      <div className="mb-8">
        <h1 className="mb-2 text-4xl font-bold tracking-tight">{t('product_form.edit_title')}</h1>
        <p className="text-muted-foreground text-[0.95rem]">
          {t('product_form.update_subtitle', { name: product.name })}
        </p>
      </div>

      <ProductForm
        defaultValues={{
          name: product.name,
          caloriesPer100g: product.caloriesPer100g,
          proteinPer100g: product.proteinPer100g,
          carbsPer100g: product.carbsPer100g,
          fatPer100g: product.fatPer100g,
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
