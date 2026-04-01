import { zodResolver } from '@hookform/resolvers/zod';
import { useProducts } from '@modules/diet-planner/api/hooks/useProducts';
import {
  Button,
  Input,
  Label,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Textarea,
} from '@shared/components/ui';
import { Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const ingredientSchema = z.object({
  productId: z.string(),
  productName: z.string(),
  amount: z.number().positive('Amount must be greater than 0'),
  unit: z.string().min(1, 'Unit is required'),
});

const recipeSchema = z.object({
  name: z.string().min(1, 'Recipe name is required'),
  description: z.string().optional(),
  instructions: z.string().optional(),
  servings: z.number().int().min(1, 'Servings must be at least 1'),
  prepTimeMinutes: z.number().int().min(0).optional(),
  ingredients: z
    .array(ingredientSchema)
    .min(1, 'At least one ingredient is required')
    .refine(
      (items) => items.every((item) => item.productId.length > 0 || item.productName.length > 0),
      { message: 'Each ingredient must have a product selected' },
    ),
});

export type RecipeFormData = z.infer<typeof recipeSchema>;

interface RecipeFormProps {
  defaultValues?: Partial<RecipeFormData>;
  onSubmit: (data: RecipeFormData) => void | Promise<void>;
  isSubmitting?: boolean;
  submitLabel?: string;
}

export function RecipeForm({
  defaultValues,
  onSubmit,
  isSubmitting,
  submitLabel,
}: RecipeFormProps) {
  const { t } = useTranslation();
  const [productSearch, setProductSearch] = useState<Record<number, string>>({});

  const {
    register,
    handleSubmit,
    control,
    watch,
    formState: { errors },
  } = useForm<RecipeFormData>({
    resolver: zodResolver(recipeSchema),
    defaultValues: {
      servings: 1,
      ingredients: [{ productId: '', productName: '', amount: 0, unit: 'g' }],
      ...defaultValues,
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'ingredients',
  });

  const watchedServings = watch('servings') || 1;

  const { data: productResults } = useProducts({
    search: Object.values(productSearch).find((s) => s) ?? '',
    onlyMine: false,
    page: 1,
    pageSize: 10,
  });

  return (
    <form
      onSubmit={(e) => {
        // REASON: zod .refine on ingredients changes the inferred output type, but runtime data shape is identical
        // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-argument
        void handleSubmit(onSubmit as any)(e);
      }}
      className="stagger-children space-y-6"
    >
      <Card>
        <CardHeader>
          <CardTitle>{t('recipe_form.basic_info')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label htmlFor="name">{t('recipe_form.name_label')}</Label>
            <Input
              id="name"
              {...register('name')}
              placeholder={t('recipe_form.name_placeholder')}
            />
            {errors.name && (
              <p className="text-destructive mt-1.5 text-sm">{errors.name.message}</p>
            )}
          </div>

          <div>
            <Label htmlFor="description">{t('recipe_form.description_label')}</Label>
            <Textarea
              id="description"
              {...register('description')}
              placeholder={t('recipe_form.description_placeholder')}
              rows={3}
            />
            {errors.description && (
              <p className="text-destructive mt-1.5 text-sm">{errors.description.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label htmlFor="servings">{t('recipe_form.servings_label')}</Label>
              <Input
                id="servings"
                type="number"
                {...register('servings', { valueAsNumber: true })}
                placeholder="1"
              />
              {errors.servings && (
                <p className="text-destructive mt-1.5 text-sm">{errors.servings.message}</p>
              )}
            </div>

            <div>
              <Label htmlFor="prepTimeMinutes">{t('recipe_form.prep_time_label')}</Label>
              <Input
                id="prepTimeMinutes"
                type="number"
                {...register('prepTimeMinutes', {
                  setValueAs: (v: string) => (v === '' ? undefined : Number(v)),
                })}
                placeholder="30"
              />
              {errors.prepTimeMinutes && (
                <p className="text-destructive mt-1.5 text-sm">{errors.prepTimeMinutes.message}</p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>{t('recipe_form.ingredients_header')}</CardTitle>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                append({ productId: '', productName: '', amount: 0, unit: 'g' });
              }}
            >
              <Plus className="mr-2 h-4 w-4" />
              {t('recipe_form.add_ingredient')}
            </Button>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {fields.map((field, index) => {
            const idx = index;
            const idxStr = String(index);
            const { onChange: onProductNameChange, ...productNameProps } = register(
              // eslint-disable-next-line @typescript-eslint/restrict-template-expressions -- RHF requires number index
              `ingredients.${idx}.productName`,
            );
            return (
              <div key={field.id} className="flex items-start gap-3">
                <div className="grid flex-1 grid-cols-3 gap-3">
                  <div>
                    <Label htmlFor={`ingredients.${idxStr}.productName`}>
                      {t('recipe_form.product_label')}
                    </Label>
                    <Input
                      {...productNameProps}
                      placeholder={t('recipe_form.product_placeholder')}
                      list={`products-${idxStr}`}
                      onChange={(e) => {
                        void onProductNameChange(e);
                        setProductSearch((prev) => ({ ...prev, [index]: e.target.value }));
                      }}
                    />
                    <datalist id={`products-${idxStr}`}>
                      {productResults?.items.map((product) => (
                        <option key={product.id} value={product.name} />
                      ))}
                    </datalist>
                    {errors.ingredients?.[index]?.productName && (
                      <p className="text-destructive mt-1.5 text-sm">
                        {/* eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- false positive: checked above */}
                        {errors.ingredients[index]?.productName.message}
                      </p>
                    )}
                  </div>

                  <div>
                    <Label htmlFor={`ingredients.${idxStr}.amount`}>
                      {t('recipe_form.amount_label')}
                    </Label>
                    <Input
                      type="number"
                      step="0.1"
                      // eslint-disable-next-line @typescript-eslint/restrict-template-expressions -- RHF requires number index
                      {...register(`ingredients.${idx}.amount`, { valueAsNumber: true })}
                      placeholder="100"
                    />
                    {errors.ingredients?.[index]?.amount && (
                      <p className="text-destructive mt-1.5 text-sm">
                        {/* eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- false positive: checked above */}
                        {errors.ingredients[index]?.amount.message}
                      </p>
                    )}
                  </div>

                  <div>
                    <Label htmlFor={`ingredients.${idxStr}.unit`}>
                      {t('recipe_form.unit_label')}
                    </Label>
                    <select
                      // eslint-disable-next-line @typescript-eslint/restrict-template-expressions -- RHF requires number index
                      {...register(`ingredients.${idx}.unit`)}
                      className="border-input bg-background ring-offset-background focus-visible:ring-ring flex h-11 w-full rounded-lg border px-4 py-2.5 text-[0.9rem] transition-all duration-200 focus-visible:ring-2 focus-visible:outline-none"
                    >
                      <option value="g">{t('product_form.units.g')}</option>
                      <option value="ml">{t('product_form.units.ml')}</option>
                      <option value="piece">{t('product_form.units.piece')}</option>
                    </select>
                    {errors.ingredients?.[index]?.unit && (
                      <p className="text-destructive mt-1.5 text-sm">
                        {/* eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- false positive: checked above */}
                        {errors.ingredients[index]?.unit.message}
                      </p>
                    )}
                  </div>
                </div>

                {fields.length > 1 && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => {
                      remove(index);
                    }}
                    className="hover:text-destructive mt-8"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                )}
              </div>
            );
          })}

          {errors.ingredients && typeof errors.ingredients.message === 'string' && (
            <p className="text-destructive text-sm">{errors.ingredients.message}</p>
          )}

          {fields.length === 0 && (
            <div className="text-muted-foreground py-10 text-center">
              <p className="mb-3">{t('recipe_form.no_ingredients')}</p>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => {
                  append({ productId: '', productName: '', amount: 0, unit: 'g' });
                }}
              >
                <Plus className="mr-2 h-4 w-4" />
                {t('recipe_form.add_first_ingredient')}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('recipe_form.instructions_header')}</CardTitle>
        </CardHeader>
        <CardContent>
          <Label htmlFor="instructions">{t('recipe_form.instructions_label')}</Label>
          <Textarea
            id="instructions"
            {...register('instructions')}
            placeholder={t('recipe_form.instructions_placeholder')}
            rows={6}
          />
          {errors.instructions && (
            <p className="text-destructive mt-1.5 text-sm">{errors.instructions.message}</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('recipe_form.summary_header')}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('recipe_form.total_ingredients')}</span>
              <span className="font-semibold">{fields.length}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('recipe_form.servings_summary')}</span>
              <span className="font-semibold">{watchedServings}</span>
            </div>
            {watch('prepTimeMinutes') && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('recipe_form.prep_time_summary')}</span>
                <span className="font-semibold">
                  {watch('prepTimeMinutes')} {t('recipes.prep_time')}
                </span>
              </div>
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
