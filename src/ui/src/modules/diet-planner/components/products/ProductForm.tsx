import { zodResolver } from '@hookform/resolvers/zod';
import { Banner, Button, Card } from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import { useForm, useWatch, type FieldError } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';

import type { ReactNode } from 'react';

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

const MACRO_LABEL_CLASS: Record<string, string> = {
  proteinPer100g: 'text-protein',
  carbsPer100g: 'text-carbs',
  fatPer100g: 'text-fat',
  fiberPer100g: 'text-fiber',
};

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
    defaultValues: { defaultUnit: 'g', ...defaultValues },
  });

  const calories = useWatch({ control, name: 'caloriesPer100g' }) || 0;
  const protein = useWatch({ control, name: 'proteinPer100g' }) || 0;
  const carbs = useWatch({ control, name: 'carbsPer100g' }) || 0;
  const fat = useWatch({ control, name: 'fatPer100g' }) || 0;

  const pCal = protein * 4;
  const cCal = carbs * 4;
  const fCal = fat * 9;
  const macroCal = pCal + cCal + fCal;
  const total = macroCal || 1;
  const pct = (n: number): number => Math.round((n / total) * 100);

  const overWeight = protein + carbs + fat > 100;
  const reconciles =
    calories > 0 && macroCal > 0 ? Math.abs(macroCal - calories) / calories <= 0.15 : true;

  return (
    <form
      onSubmit={(e) => {
        void handleSubmit(onSubmit)(e);
      }}
      className="grid gap-[18px] lg:grid-cols-3"
    >
      <Card className="flex flex-col gap-5 p-6 lg:col-span-2">
        <div>
          <div className="mb-3 text-[15px] font-bold">{t('product_form.basic_info')}</div>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field id="name" label={t('product_form.name_label')} error={errors.name}>
              <input
                id="name"
                {...register('name')}
                placeholder={t('product_form.name_placeholder')}
                className={inputClass(!!errors.name)}
              />
            </Field>
            <Field id="defaultUnit" label={t('product_form.unit_label')} error={errors.defaultUnit}>
              <select
                id="defaultUnit"
                {...register('defaultUnit')}
                className={inputClass(!!errors.defaultUnit)}
              >
                <option value="g">{t('product_form.units.g')}</option>
                <option value="ml">{t('product_form.units.ml')}</option>
                <option value="piece">{t('product_form.units.piece')}</option>
              </select>
            </Field>
          </div>
        </div>

        <div className="border-border border-t pt-5">
          <div className="mb-3 text-[15px] font-bold">{t('product_form.nutrition_header')}</div>
          <div className="grid gap-4 sm:grid-cols-3">
            <NumField
              id="caloriesPer100g"
              label={t('product_form.calories_label')}
              register={register}
              error={errors.caloriesPer100g}
            />
            <NumField
              id="proteinPer100g"
              label={t('product_form.protein_label')}
              register={register}
              error={errors.proteinPer100g}
            />
            <NumField
              id="carbsPer100g"
              label={t('product_form.carbs_label')}
              register={register}
              error={errors.carbsPer100g}
            />
            <NumField
              id="fatPer100g"
              label={t('product_form.fat_label')}
              register={register}
              error={errors.fatPer100g}
            />
            <NumField
              id="fiberPer100g"
              label={t('product_form.fiber_label')}
              register={register}
              error={errors.fiberPer100g}
              optional
            />
          </div>
        </div>

        <div className="border-border border-t pt-5">
          <div className="mb-3 text-[15px] font-bold">{t('product_form.conversions_header')}</div>
          <div className="grid gap-4 sm:grid-cols-2">
            <NumField
              id="densityGramsPerMl"
              label={t('product_form.density_label')}
              register={register}
              error={errors.densityGramsPerMl}
              optional
              hint={t('product_form.density_help')}
              step="0.01"
            />
            <NumField
              id="gramPerPiece"
              label={t('product_form.piece_label')}
              register={register}
              error={errors.gramPerPiece}
              optional
              hint={t('product_form.piece_help')}
            />
          </div>
        </div>

        <div className="border-border flex justify-end gap-3 border-t pt-5">
          <Button
            type="button"
            variant="outline"
            size="xl"
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
      </Card>

      <div className="flex flex-col gap-[18px]">
        <Card className="flex flex-col gap-3 p-[22px]">
          <div className="text-[15px] font-bold">{t('product_form.macro_summary')}</div>
          <div className="flex h-3 overflow-hidden rounded-full">
            {(['protein', 'carbs', 'fat'] as const).map((k, i) => (
              <div
                key={k}
                style={{
                  width: `${String(pct(i === 0 ? pCal : i === 1 ? cCal : fCal))}%`,
                  background: `var(--color-${k})`,
                }}
              />
            ))}
          </div>
          <div className="flex flex-col">
            {(
              [
                ['protein', t('products.table.protein'), protein, pCal],
                ['carbs', t('products.table.carbs'), carbs, cCal],
                ['fat', t('products.table.fat'), fat, fCal],
              ] as const
            ).map(([k, label, grams, cal]) => (
              <div key={k} className="flex items-center justify-between py-1.5 text-[12.5px]">
                <span className="inline-flex items-center gap-2 font-semibold">
                  <span
                    className="size-2.5 rounded-full"
                    style={{ background: `var(--color-${k})` }}
                  />
                  {label}
                </span>
                <span className="text-text-2 tnum">
                  {grams.toFixed(1)} g · {pct(cal)}%
                </span>
              </div>
            ))}
          </div>

          {overWeight ? (
            <Banner variant="warning">
              {t('product_form.macro_warning', { total: (protein + carbs + fat).toFixed(1) })}
            </Banner>
          ) : reconciles ? (
            <Banner variant="success">{t('product_form.macros_reconcile')}</Banner>
          ) : (
            <Banner variant="warning" title={t('product_form.macros_mismatch_title')}>
              {t('product_form.macros_mismatch', {
                macroCal: Math.round(macroCal),
                calories: Math.round(calories),
              })}
            </Banner>
          )}
        </Card>
      </div>
    </form>
  );
}

function inputClass(invalid: boolean): string {
  return cn(
    'h-[42px] w-full rounded-[13px] border px-3 text-[13px] outline-none transition-colors focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]',
    invalid ? 'border-destructive' : 'border-border bg-secondary',
  );
}

function Field({
  id,
  label,
  labelClassName,
  error,
  hint,
  children,
}: {
  id: string;
  label: string;
  labelClassName?: string;
  error?: FieldError | undefined;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <div>
      <label
        htmlFor={id}
        className={cn(
          'mb-1 block text-[12px] font-semibold',
          error ? 'text-destructive' : (labelClassName ?? 'text-text-2'),
        )}
      >
        {label}
      </label>
      <div
        style={
          error
            ? {
                background: 'color-mix(in oklab, var(--color-fat) 7%, transparent)',
                borderRadius: 13,
              }
            : undefined
        }
      >
        {children}
      </div>
      {error ? (
        <p className="text-destructive mt-1 text-[11.5px]">{error.message}</p>
      ) : hint ? (
        <p className="text-muted-foreground mt-1 text-[11.5px]">{hint}</p>
      ) : null}
    </div>
  );
}

function NumField({
  id,
  label,
  register,
  error,
  optional = false,
  hint,
  step = '0.1',
}: {
  id: keyof ProductFormData;
  label: string;
  register: ReturnType<typeof useForm<ProductFormData>>['register'];
  error?: FieldError | undefined;
  optional?: boolean;
  hint?: string;
  step?: string;
}) {
  const opts = optional
    ? { setValueAs: (v: string) => (v === '' ? undefined : Number(v)) }
    : { valueAsNumber: true };
  return (
    <Field
      id={id}
      label={label}
      error={error}
      {...(hint ? { hint } : {})}
      {...(MACRO_LABEL_CLASS[id] ? { labelClassName: MACRO_LABEL_CLASS[id] } : {})}
    >
      <input
        id={id}
        type="number"
        step={step}
        placeholder="0"
        {...register(id, opts)}
        className={cn(inputClass(!!error), 'tnum')}
      />
    </Field>
  );
}
