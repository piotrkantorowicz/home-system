import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Button,
  Input,
  Label,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@shared/components/ui';

const productSchema = z.object({
  name: z.string().min(1, 'Product name is required'),
  caloriesPer100g: z.number().min(0, 'Calories must be 0 or greater'),
  proteinPer100g: z.number().min(0, 'Protein must be 0 or greater'),
  carbsPer100g: z.number().min(0, 'Carbs must be 0 or greater'),
  fatPer100g: z.number().min(0, 'Fat must be 0 or greater'),
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
    watch,
    formState: { errors },
  } = useForm<ProductFormData>({
    resolver: zodResolver(productSchema),
    defaultValues: {
      defaultUnit: 'g',
      ...defaultValues,
    },
  });

  const protein = watch('proteinPer100g') || 0;
  const carbs = watch('carbsPer100g') || 0;
  const fat = watch('fatPer100g') || 0;

  const totalMacros = Number(protein) + Number(carbs) + Number(fat);
  const isMacroWarning = totalMacros > 100;

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6 stagger-children">
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
              <p className="mt-1.5 text-sm text-destructive">{errors.name.message}</p>
            )}
          </div>

          <div>
            <Label htmlFor="defaultUnit">{t('product_form.unit_label')}</Label>
            <select
              id="defaultUnit"
              {...register('defaultUnit')}
              className="flex h-11 w-full rounded-lg border border-input bg-background px-4 py-2.5 text-[0.9rem] ring-offset-background transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <option value="g">{t('product_form.units.g')}</option>
              <option value="ml">{t('product_form.units.ml')}</option>
              <option value="piece">{t('product_form.units.piece')}</option>
            </select>
            {errors.defaultUnit && (
              <p className="mt-1.5 text-sm text-destructive">{errors.defaultUnit.message}</p>
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
                <p className="mt-1.5 text-sm text-destructive">{errors.caloriesPer100g.message}</p>
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
                <p className="mt-1.5 text-sm text-destructive">{errors.proteinPer100g.message}</p>
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
                <p className="mt-1.5 text-sm text-destructive">{errors.carbsPer100g.message}</p>
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
                <p className="mt-1.5 text-sm text-destructive">{errors.fatPer100g.message}</p>
              )}
            </div>
          </div>

          {isMacroWarning && (
            <div className="rounded-xl border border-destructive/30 bg-destructive/5 p-4">
              <p className="text-sm text-destructive">
                {t('product_form.macro_warning', { total: totalMacros.toFixed(1) })}
              </p>
            </div>
          )}

          <div className="rounded-xl bg-muted/50 p-5">
            <p className="text-sm font-semibold mb-3">{t('product_form.macro_summary')}</p>
            <div className="grid grid-cols-3 gap-4">
              <div>
                <p className="text-xs text-muted-foreground uppercase tracking-wider">
                  {t('products.table.protein')}
                </p>
                <p className="text-lg font-semibold mt-0.5">{Number(protein).toFixed(1)}g</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground uppercase tracking-wider">
                  {t('products.table.carbs')}
                </p>
                <p className="text-lg font-semibold mt-0.5">{Number(carbs).toFixed(1)}g</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground uppercase tracking-wider">
                  {t('products.table.fat')}
                </p>
                <p className="text-lg font-semibold mt-0.5">{Number(fat).toFixed(1)}g</p>
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
                setValueAs: (v) => (v === '' || isNaN(v) ? undefined : Number(v)),
              })}
              placeholder={t('product_form.density_placeholder')}
            />
            <p className="mt-1.5 text-xs text-muted-foreground">{t('product_form.density_help')}</p>
            {errors.densityGramsPerMl && (
              <p className="mt-1.5 text-sm text-destructive">{errors.densityGramsPerMl.message}</p>
            )}
          </div>

          <div>
            <Label htmlFor="gramPerPiece">{t('product_form.piece_label')}</Label>
            <Input
              id="gramPerPiece"
              type="number"
              step="0.1"
              {...register('gramPerPiece', {
                setValueAs: (v) => (v === '' || isNaN(v) ? undefined : Number(v)),
              })}
              placeholder={t('product_form.piece_placeholder')}
            />
            <p className="mt-1.5 text-xs text-muted-foreground">{t('product_form.piece_help')}</p>
            {errors.gramPerPiece && (
              <p className="mt-1.5 text-sm text-destructive">{errors.gramPerPiece.message}</p>
            )}
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-3 pt-2">
        <Button type="button" variant="outline" onClick={() => window.history.back()}>
          {t('common.cancel')}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? t('product_form.saving') : submitLabel || t('common.save')}
        </Button>
      </div>
    </form>
  );
}
