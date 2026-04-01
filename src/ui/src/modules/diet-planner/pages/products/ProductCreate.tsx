import { useCreateProduct } from '@modules/diet-planner/api/hooks/useProducts';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { ProductForm, type ProductFormData } from '../../components/products/ProductForm';

export default function ProductCreate() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const createMutation = useCreateProduct();

  const handleSubmit = async (data: ProductFormData) => {
    try {
      await createMutation.mutateAsync({
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
      void navigate('/diet-planner/products');
    } catch (error) {
      console.error('Failed to create product:', error);
    }
  };

  return (
    <div className="animate-fade-in-up mx-auto max-w-4xl p-8 lg:p-10">
      <div className="mb-8">
        <h1 className="mb-2 text-4xl font-bold tracking-tight">{t('product_form.create_title')}</h1>
        <p className="text-muted-foreground text-[0.95rem]">{t('product_form.create_subtitle')}</p>
      </div>

      <ProductForm
        onSubmit={handleSubmit}
        isSubmitting={createMutation.isPending}
        submitLabel={t('product_form.create_btn')}
      />
    </div>
  );
}
