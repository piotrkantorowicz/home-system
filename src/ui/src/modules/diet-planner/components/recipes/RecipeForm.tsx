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
  Select,
  Textarea,
} from '@shared/components/ui';
import { Plus, Trash2 } from 'lucide-react';
import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useForm, useFieldArray, useWatch } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

const ingredientSchema = z.object({
  productId: z.string().min(1, 'Select a product from the list'),
  productName: z.string().min(1, 'Select a product from the list'),
  amount: z.number().positive('Amount must be greater than 0'),
  unit: z.string().min(1, 'Unit is required'),
});

const recipeSchema = z.object({
  name: z.string().min(1, 'Recipe name is required'),
  description: z.string().optional(),
  instructions: z.string().optional(),
  servings: z.number().int().min(1, 'Servings must be at least 1'),
  prepTimeMinutes: z.number().int().min(0).optional(),
  ingredients: z.array(ingredientSchema).min(1, 'At least one ingredient is required'),
});

export type RecipeFormData = z.infer<typeof recipeSchema>;

interface RecipeFormProps {
  defaultValues?: Partial<RecipeFormData>;
  onSubmit: (data: RecipeFormData) => void | Promise<void>;
  isSubmitting?: boolean;
  submitLabel?: string;
}

interface ProductPickerProps {
  value: { productId: string; productName: string };
  onChange: (next: { productId: string; productName: string }) => void;
  placeholder?: string;
  invalid?: boolean;
  inputId?: string;
}

function ProductPicker({ value, onChange, placeholder, invalid, inputId }: ProductPickerProps) {
  // Local input state — the form's productId stays untouched until the user actually
  // picks an option, so typing without selecting and then blurring leaves the row alone.
  const [inputValue, setInputValue] = useState(value.productName);
  const [lastCommittedName, setLastCommittedName] = useState(value.productName);
  if (value.productName !== lastCommittedName) {
    setLastCommittedName(value.productName);
    setInputValue(value.productName);
  }
  const [isOpen, setIsOpen] = useState(false);
  const blurTimeoutRef = useRef<number | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const [popoverRect, setPopoverRect] = useState<{
    top: number;
    left: number;
    width: number;
  } | null>(null);

  useEffect(
    () => () => {
      if (blurTimeoutRef.current !== null) window.clearTimeout(blurTimeoutRef.current);
    },
    [],
  );

  useLayoutEffect(() => {
    if (!isOpen) return;
    const updateRect = () => {
      const input = inputRef.current;
      if (!input) return;
      const rect = input.getBoundingClientRect();
      setPopoverRect({ top: rect.bottom + 4, left: rect.left, width: rect.width });
    };
    updateRect();
    window.addEventListener('scroll', updateRect, true);
    window.addEventListener('resize', updateRect);
    return () => {
      window.removeEventListener('scroll', updateRect, true);
      window.removeEventListener('resize', updateRect);
    };
  }, [isOpen]);

  const { data: results } = useProducts({
    search: inputValue,
    onlyMine: false,
    page: 1,
    pageSize: 10,
  });
  const products = results?.items ?? [];

  const commit = (product: { id: string; name: string }) => {
    onChange({ productId: product.id, productName: product.name });
    setInputValue(product.name);
    setIsOpen(false);
  };

  const handleBlur = () => {
    blurTimeoutRef.current = window.setTimeout(() => {
      const exact = products.find((p) => p.name === inputValue);
      if (exact && exact.id !== value.productId) {
        commit(exact);
      } else {
        // No new selection: snap the input back to whatever was last committed.
        // Form state (productId/productName) is untouched, so a previously picked
        // product stays picked.
        setInputValue(value.productName);
      }
      setIsOpen(false);
    }, 150);
  };

  const handleFocus = () => {
    if (blurTimeoutRef.current !== null) {
      window.clearTimeout(blurTimeoutRef.current);
      blurTimeoutRef.current = null;
    }
    setIsOpen(true);
  };

  return (
    <div className="relative">
      <Input
        ref={inputRef}
        id={inputId}
        value={inputValue}
        placeholder={placeholder}
        autoComplete="off"
        aria-invalid={invalid}
        aria-autocomplete="list"
        aria-expanded={isOpen}
        role="combobox"
        onChange={(e) => {
          setInputValue(e.target.value);
          setIsOpen(true);
        }}
        onFocus={handleFocus}
        onBlur={handleBlur}
      />
      {isOpen &&
        products.length > 0 &&
        popoverRect &&
        createPortal(
          <ul
            role="listbox"
            className="max-h-60 overflow-auto rounded-lg border shadow-lg"
            style={{
              position: 'fixed',
              top: popoverRect.top,
              left: popoverRect.left,
              width: popoverRect.width,
              zIndex: 60,
              background: 'var(--color-popover)',
              color: 'var(--color-popover-foreground)',
              borderColor: 'var(--color-border)',
            }}
          >
            {products.map((product) => (
              <li key={product.id}>
                <button
                  type="button"
                  role="option"
                  aria-selected={product.id === value.productId}
                  className="hover:bg-accent hover:text-accent-foreground w-full px-4 py-2 text-left text-sm"
                  onMouseDown={(e) => {
                    // Prevent the input's blur from firing before we commit.
                    e.preventDefault();
                    commit(product);
                  }}
                >
                  {product.name}
                </button>
              </li>
            ))}
          </ul>,
          document.body,
        )}
    </div>
  );
}

export function RecipeForm({
  defaultValues,
  onSubmit,
  isSubmitting,
  submitLabel,
}: RecipeFormProps) {
  const { t } = useTranslation();

  const {
    register,
    handleSubmit,
    control,
    setValue,
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

  const watchedServings = useWatch({ control, name: 'servings' }) || 1;
  const watchedPrepTime = useWatch({ control, name: 'prepTimeMinutes' });
  const watchedIngredients = useWatch({ control, name: 'ingredients' });

  return (
    <form
      onSubmit={(e) => {
        void handleSubmit(onSubmit)(e);
      }}
      className="mx-auto flex max-w-3xl flex-col gap-[18px]"
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
            const row = watchedIngredients[idx];
            return (
              <div key={field.id} className="flex items-start gap-3">
                <div className="grid flex-1 gap-3 sm:grid-cols-[2fr_0.8fr_0.8fr]">
                  <div>
                    <Label htmlFor={`ingredients.${idxStr}.productName`}>
                      {t('recipe_form.product_label')}
                    </Label>
                    <ProductPicker
                      inputId={`ingredients.${idxStr}.productName`}
                      placeholder={t('recipe_form.product_placeholder')}
                      invalid={!!errors.ingredients?.[index]?.productId}
                      value={{
                        productId: row?.productId ?? '',
                        productName: row?.productName ?? '',
                      }}
                      onChange={(next) => {
                        // eslint-disable-next-line @typescript-eslint/restrict-template-expressions -- RHF requires number index
                        setValue(`ingredients.${idx}.productName`, next.productName, {
                          shouldValidate: true,
                        });
                        // eslint-disable-next-line @typescript-eslint/restrict-template-expressions -- RHF requires number index
                        setValue(`ingredients.${idx}.productId`, next.productId, {
                          shouldValidate: true,
                        });
                      }}
                    />
                    {errors.ingredients?.[index]?.productId && (
                      <p className="text-destructive mt-1.5 text-sm">
                        {/* eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- false positive: checked above */}
                        {errors.ingredients[index]?.productId.message}
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
                    <Select
                      id={`ingredients.${idxStr}.unit`}
                      // eslint-disable-next-line @typescript-eslint/restrict-template-expressions -- RHF requires number index
                      {...register(`ingredients.${idx}.unit`)}
                    >
                      <option value="g">{t('product_form.units.g')}</option>
                      <option value="ml">{t('product_form.units.ml')}</option>
                      <option value="piece">{t('product_form.units.piece')}</option>
                    </Select>
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
          <div className="space-y-3 text-[13px]">
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('recipe_form.total_ingredients')}</span>
              <span className="tnum font-semibold">{fields.length}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('recipe_form.servings_summary')}</span>
              <span className="tnum font-semibold">{watchedServings}</span>
            </div>
            {watchedPrepTime !== undefined && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('recipe_form.prep_time_summary')}</span>
                <span className="tnum font-semibold">
                  {watchedPrepTime} {t('recipes.prep_time')}
                </span>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-3 pt-2">
        <Button
          type="button"
          size="xl"
          variant="outline"
          onClick={() => {
            window.history.back();
          }}
        >
          {t('common.cancel')}
        </Button>
        <Button type="submit" size="xl" disabled={isSubmitting}>
          {isSubmitting ? t('product_form.saving') : (submitLabel ?? t('common.save'))}
        </Button>
      </div>
    </form>
  );
}
