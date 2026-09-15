import { useCreateProduct } from '@modules/diet-planner/api/hooks/useProducts';
import { useToast } from '@shared/context/ToastContext';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { ProductForm, type ProductFormData } from '../../components/products/ProductForm';

export default function ProductCreate() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const toast = useToast();
  const createMutation = useCreateProduct();

  const handleSubmit = async (data: ProductFormData) => {
    const request = {
      name: data.name,
      calories: data.caloriesPer100g,
      protein: data.proteinPer100g,
      carbs: data.carbsPer100g,
      fat: data.fatPer100g,
      fiber: data.fiberPer100g ?? null,
      defaultUnit: data.defaultUnit,
      densityGramsPerMl: data.densityGramsPerMl ?? null,
      gramPerPiece: data.gramPerPiece ?? null,
    };
    try {
      await createMutation.mutateAsync(request);
    } catch {
      toast.error(t('product_form.create_error'));
      return;
    }
    toast.success(t('product_form.create_success'));
    void navigate('/diet-planner/products');
  };

  return (
    <div className="animate-fade-in-up mx-auto max-w-6xl px-4 py-6 md:px-8">
      <div className="mb-8">
        <h1 className="text-[26px] font-bold">{t('product_form.create_title')}</h1>
        <p className="text-muted-foreground mt-1 text-sm">{t('product_form.create_subtitle')}</p>
      </div>

      <ProductForm
        onSubmit={handleSubmit}
        isSubmitting={createMutation.isPending}
        submitLabel={t('product_form.create_btn')}
      />
    </div>
  );
}
