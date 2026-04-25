import { zodResolver } from '@hookform/resolvers/zod';
import {
  Button,
  Input,
  Label,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@shared/components/ui';
import { useForm, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const productSchema = z.object({
  name: z.string().min(1, 'Product name is required'),
  caloriesPer100g: z.number().min(0, 'Calories must be 0 or greater'),
  proteinPer100g: z.number().min(0, 'Protein must be 0 or greater'),
  carbsPer100g: z.number().min(0, 'Carbs must be 0 or greater'),
  fatPer100g: z.number().min(0, 'Fat must be 0 or greater'),
  fiberPer100g: z.number().min(0, 'Fiber must be 0 or greater').optional(),
  defaultUnit: z.string(),
  densityGramsPerMl: z.number().positive().optional(),
  gramPerPiece: z.number().positive().optional(),
});

export type ProductFormData = z.infer<typeof productSchema>;

interface ProductFormProps {
  defaultValues?: Partial<ProductFormData>;
  onSubmit: (data: ProductFormData) => void | Promise<void>;
  isSubmitting?: boolean;
  submitLabel?: string;
}

export function ProductForm({
  defaultValues,
  onSubmit,
  isSubmitting,
  submitLabel,
}: ProductFormProps) {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm<ProductFormData>({
    resolver: zodResolver(productSchema),
    defaultValues: {
      defaultUnit: 'g',
      ...defaultValues,
    },
  });

  const protein = useWatch({ control, name: 'proteinPer100g' }) || 0;
  const carbs = useWatch({ control, name: 'carbsPer100g' }) || 0;
  const fat = useWatch({ control, name: 'fatPer100g' }) || 0;
  const fiber = Number(useWatch({ control, name: 'fiberPer100g' })) || 0;

  const totalMacros = protein + carbs + fat;
  const isMacroWarning = totalMacros > 100;

  return (
    <form
      onSubmit={(e) => {
        void handleSubmit(onSubmit)(e);
      }}
      className="stagger-children space-y-6"
    >
      <Card>
        <CardHeader>
          <CardTitle>{t('product_form.basic_info')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label htmlFor="name">{t('product_form.name_label')}</Label>
            <Input
              id="name"
              {...register('name')}
              placeholder={t('product_form.name_placeholder')}
            />
            {errors.name && (
              <p className="text-destructive mt-1.5 text-sm">{errors.name.message}</p>
            )}
          </div>

          <div>
            <Label htmlFor="defaultUnit">{t('product_form.unit_label')}</Label>
            <select
              id="defaultUnit"
              {...register('defaultUnit')}
              className="border-input bg-background ring-offset-background focus-visible:ring-ring flex h-11 w-full rounded-lg border px-4 py-2.5 text-[0.9rem] transition-all duration-200 focus-visible:ring-2 focus-visible:outline-none"
            >
              <option value="g">{t('product_form.units.g')}</option>
              <option value="ml">{t('product_form.units.ml')}</option>
              <option value="piece">{t('product_form.units.piece')}</option>
            </select>
            {errors.defaultUnit && (
              <p className="text-destructive mt-1.5 text-sm">{errors.defaultUnit.message}</p>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('product_form.nutrition_header')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="caloriesPer100g">{t('product_form.calories_label')}</Label>
              <Input
                id="caloriesPer100g"
                type="number"
                step="0.1"
                {...register('caloriesPer100g', { valueAsNumber: true })}
                placeholder="0"
              />
              {errors.caloriesPer100g && (
                <p className="text-destructive mt-1.5 text-sm">{errors.caloriesPer100g.message}</p>
              )}
            </div>

            <div>
              <Label htmlFor="proteinPer100g">{t('product_form.protein_label')}</Label>
              <Input
                id="proteinPer100g"
                type="number"
                step="0.1"
                {...register('proteinPer100g', { valueAsNumber: true })}
                placeholder="0"
              />
              {errors.proteinPer100g && (
                <p className="text-destructive mt-1.5 text-sm">{errors.proteinPer100g.message}</p>
              )}
            </div>

            <div>
              <Label htmlFor="carbsPer100g">{t('product_form.carbs_label')}</Label>
              <Input
                id="carbsPer100g"
                type="number"
                step="0.1"
                {...register('carbsPer100g', { valueAsNumber: true })}
                placeholder="0"
              />
              {errors.carbsPer100g && (
                <p className="text-destructive mt-1.5 text-sm">{errors.carbsPer100g.message}</p>
              )}
            </div>

            <div>
              <Label htmlFor="fatPer100g">{t('product_form.fat_label')}</Label>
              <Input
                id="fatPer100g"
                type="number"
                step="0.1"
                {...register('fatPer100g', { valueAsNumber: true })}
                placeholder="0"
              />
              {errors.fatPer100g && (
                <p className="text-destructive mt-1.5 text-sm">{errors.fatPer100g.message}</p>
              )}
            </div>

            <div>
              <Label htmlFor="fiberPer100g">{t('product_form.fiber_label')}</Label>
              <Input
                id="fiberPer100g"
                type="number"
                step="0.1"
                {...register('fiberPer100g', {
                  setValueAs: (v: string) => (v === '' ? undefined : Number(v)),
                })}
                placeholder="0"
              />
              {errors.fiberPer100g && (
                <p className="text-destructive mt-1.5 text-sm">{errors.fiberPer100g.message}</p>
              )}
            </div>
          </div>

          {isMacroWarning && (
            <div className="border-destructive/30 bg-destructive/5 rounded-xl border p-4">
              <p className="text-destructive text-sm">
                {t('product_form.macro_warning', { total: totalMacros.toFixed(1) })}
              </p>
            </div>
          )}

          <div className="bg-muted/50 rounded-xl p-5">
            <p className="mb-3 text-sm font-semibold">{t('product_form.macro_summary')}</p>
            <div className="grid grid-cols-4 gap-4">
              <div>
                <p className="text-muted-foreground text-xs tracking-wider uppercase">
                  {t('products.table.protein')}
                </p>
                <p className="mt-0.5 text-lg font-semibold">{protein.toFixed(1)}g</p>
              </div>
              <div>
                <p className="text-muted-foreground text-xs tracking-wider uppercase">
                  {t('products.table.carbs')}
                </p>
                <p className="mt-0.5 text-lg font-semibold">{carbs.toFixed(1)}g</p>
              </div>
              <div>
                <p className="text-muted-foreground text-xs tracking-wider uppercase">
                  {t('products.table.fat')}
                </p>
                <p className="mt-0.5 text-lg font-semibold">{fat.toFixed(1)}g</p>
              </div>
              <div>
                <p className="text-muted-foreground text-xs tracking-wider uppercase">
                  {t('products.table.fiber')}
                </p>
                <p className="mt-0.5 text-lg font-semibold">{fiber.toFixed(1)}g</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('product_form.conversions_header')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label htmlFor="densityGramsPerMl">{t('product_form.density_label')}</Label>
            <Input
              id="densityGramsPerMl"
              type="number"
              step="0.01"
              {...register('densityGramsPerMl', {
                setValueAs: (v: string) => (v === '' ? undefined : Number(v)),
              })}
              placeholder={t('product_form.density_placeholder')}
            />
            <p className="text-muted-foreground mt-1.5 text-xs">{t('product_form.density_help')}</p>
            {errors.densityGramsPerMl && (
              <p className="text-destructive mt-1.5 text-sm">{errors.densityGramsPerMl.message}</p>
            )}
          </div>

          <div>
            <Label htmlFor="gramPerPiece">{t('product_form.piece_label')}</Label>
            <Input
              id="gramPerPiece"
              type="number"
              step="0.1"
              {...register('gramPerPiece', {
                setValueAs: (v: string) => (v === '' ? undefined : Number(v)),
              })}
              placeholder={t('product_form.piece_placeholder')}
            />
            <p className="text-muted-foreground mt-1.5 text-xs">{t('product_form.piece_help')}</p>
            {errors.gramPerPiece && (
              <p className="text-destructive mt-1.5 text-sm">{errors.gramPerPiece.message}</p>
            )}
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-3 pt-2">
        <Button
          type="button"
          variant="outline"
          onClick={() => {
            window.history.back();
          }}
        >
          {t('common.cancel')}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? t('product_form.saving') : (submitLabel ?? t('common.save'))}
        </Button>
      </div>
    </form>
  );
}
