import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ProductForm, type ProductFormData } from '../../components/products/ProductForm';
import { useCreateProduct } from '@modules/diet-planner/api/hooks/useProducts';

export default function ProductCreate() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const createMutation = useCreateProduct();

  const handleSubmit = async (data: ProductFormData) => {
    try {
      await createMutation.mutateAsync({
        name: data.name,
        caloriesPer100g: data.caloriesPer100g,
        proteinPer100g: data.proteinPer100g,
        carbsPer100g: data.carbsPer100g,
        fatPer100g: data.fatPer100g,
        fiberPer100g: data.fiberPer100g ?? null,
        defaultUnit: data.defaultUnit,
        densityGramsPerMl: data.densityGramsPerMl || null,
        gramPerPiece: data.gramPerPiece || null,
      });
      navigate('/diet-planner/products');
    } catch (error) {
      console.error('Failed to create product:', error);
    }
  };

  return (
    <div className="p-8 lg:p-10 max-w-4xl mx-auto animate-fade-in-up">
      <div className="mb-8">
        <h1 className="text-4xl font-bold tracking-tight mb-2">{t('product_form.create_title')}</h1>
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
