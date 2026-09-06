import { useValidateImport, useExecuteImport } from '@modules/diet-planner/api/hooks/useMeals';
import { Banner, Button, Card } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { cn, formatNumber } from '@shared/lib/utils';
import { Check, FileJson, Upload } from 'lucide-react';
import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import type { components } from '../../api/generated/schema';

type ImportDto = components['schemas']['ImportDto'];
type ImportProductDto = components['schemas']['ImportProductDto'];
type ValidationResultDto = components['schemas']['ValidationResultDto'];

interface ValidationState extends ValidationResultDto {
  isApiError?: boolean;
}

type Step = 'upload' | 'review' | 'done';

const num = (v: number | string | null | undefined): number =>
  typeof v === 'number' ? v : Number(v ?? 0);

const sampleJson = {
  products: [
    {
      name: 'Chicken Breast',
      caloriesPer100g: 165,
      proteinPer100g: 31,
      carbsPer100g: 0,
      fatPer100g: 3.6,
      fiberPer100g: 0,
      unit: 'g',
    },
    {
      name: 'Brown Rice',
      caloriesPer100g: 362,
      proteinPer100g: 7.5,
      carbsPer100g: 76,
      fatPer100g: 2.7,
      fiberPer100g: 3.5,
      unit: 'g',
    },
  ],
  recipes: [
    {
      name: 'Grilled Chicken with Rice',
      description: 'Simple and healthy',
      servings: 2,
      prepTimeMinutes: 30,
      ingredients: [
        { product: 'Chicken Breast', amount: 300, unit: 'g' },
        { product: 'Brown Rice', amount: 150, unit: 'g' },
      ],
      instructions: '1. Grill chicken. 2. Cook rice.',
    },
  ],
  schedule: [
    {
      date: '2026-03-18',
      meals: [{ type: 'lunch', recipe: 'Grilled Chicken with Rice', servings: 1 }],
    },
    {
      date: '2026-03-19',
      meals: [{ type: 'dinner', recipe: 'Grilled Chicken with Rice', servings: 1 }],
    },
  ],
};

export default function ImportWizard() {
  const { t } = useTranslation();
  const toast = useToast();
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [step, setStep] = useState<Step>('upload');
  const [jsonInput, setJsonInput] = useState('');
  const [jsonError, setJsonError] = useState<string | null>(null);
  const [parsed, setParsed] = useState<ImportDto | null>(null);
  const [validation, setValidation] = useState<ValidationState | null>(null);
  const [importError, setImportError] = useState<string | null>(null);

  const validateMutation = useValidateImport();
  const importMutation = useExecuteImport();

  const readFile = (file: File) => {
    if (!file.name.endsWith('.json')) {
      setJsonError(t('import_wizard.upload.not_json'));
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      setJsonInput(typeof reader.result === 'string' ? reader.result : '');
      setJsonError(null);
    };
    reader.readAsText(file);
  };

  const runValidation = async (data: ImportDto) => {
    try {
      const result = await validateMutation.mutateAsync(data);
      setValidation({ ...result, isApiError: false });
    } catch (error) {
      setValidation({
        valid: false,
        canProceed: false,
        summary: { errors: 1, warnings: 0, info: 0 },
        issues: [
          {
            severity: 'error',
            category: 'api',
            path: null,
            item: null,
            message: error instanceof Error ? error.message : t('import_wizard.review.error_hint'),
            resolution: null,
          },
        ],
        plan: null,
        isApiError: true,
      });
    }
  };

  const handleContinue = async () => {
    let data: ImportDto;
    try {
      data = JSON.parse(jsonInput) as ImportDto;
    } catch {
      setJsonError(t('import_wizard.upload.invalid_json'));
      return;
    }
    setJsonError(null);
    setParsed(data);
    await runValidation(data);
    setStep('review');
  };

  const handleImport = async () => {
    if (!parsed) return;
    setImportError(null);
    try {
      await importMutation.mutateAsync(parsed);
      setStep('done');
      setTimeout(() => {
        void navigate('/diet-planner/calendar');
      }, 2000);
    } catch {
      const msg = t('import_wizard.import_error');
      setImportError(msg);
      toast.error(msg);
    }
  };

  const products: ImportProductDto[] = parsed?.products ?? [];
  const days = parsed?.schedule?.length ?? 0;
  const mealEntries =
    num(validation?.plan?.mealEntriesToCreate) ||
    (parsed?.schedule ?? []).reduce((sum, d) => sum + (d.meals?.length ?? 0), 0);
  const newProducts = num(validation?.plan?.productsToCreate);
  const warnings = num(validation?.summary.warnings);
  const canProceed = validation?.canProceed === true;

  return (
    <div className="animate-fade-in mx-auto flex max-w-4xl flex-col gap-6 px-4 py-6 md:px-8">
      <div>
        <h1 className="text-[26px] font-bold">{t('import_wizard.title')}</h1>
        <p className="text-muted-foreground mt-1 text-sm">{t('import_wizard.subtitle')}</p>
      </div>

      <Card className="overflow-hidden p-0">
        <StepBar step={step} />

        {step === 'upload' ? (
          <div className="grid gap-5 p-6 md:grid-cols-2">
            <div className="flex flex-col gap-4">
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                className="border-border-strong bg-secondary text-muted-foreground hover:text-foreground hover:border-foreground/40 flex flex-col items-center gap-2 rounded-[18px] border border-dashed p-7 text-center transition-colors"
              >
                <Upload className="size-6" strokeWidth={1.9} />
                <span className="text-[13px] font-semibold">
                  {t('import_wizard.upload.dropzone_title')}
                </span>
                <span className="text-[11.5px]">{t('import_wizard.upload.dropzone_hint')}</span>
              </button>
              <input
                ref={fileInputRef}
                type="file"
                accept=".json,application/json"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) readFile(file);
                }}
              />

              <textarea
                aria-label={t('import_wizard.upload.paste_label')}
                value={jsonInput}
                onChange={(e) => {
                  setJsonInput(e.target.value);
                }}
                placeholder={t('import_wizard.upload.placeholder')}
                rows={10}
                className="border-border bg-muted rounded-[16px] border p-3 font-mono text-[11.5px] leading-relaxed outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]"
              />
              {jsonError ? <Banner variant="error">{jsonError}</Banner> : null}
              <div className="flex flex-wrap gap-2">
                <Button
                  variant="outline"
                  size="xs"
                  onClick={() => {
                    setJsonInput(JSON.stringify(sampleJson, null, 2));
                  }}
                >
                  {t('import_wizard.upload.load_sample')}
                </Button>
              </div>
            </div>

            <div className="flex flex-col gap-3">
              <div className="text-text-2 text-[13px] font-bold">
                {t('import_wizard.upload.expected_format')}
              </div>
              <pre className="bg-muted max-h-[280px] overflow-auto rounded-[16px] p-3 font-mono text-[11.5px] leading-relaxed whitespace-pre">
                {JSON.stringify(sampleJson, null, 2)}
              </pre>
            </div>

            <div className="flex justify-end md:col-span-2">
              <Button
                size="xl"
                onClick={() => {
                  void handleContinue();
                }}
                disabled={!jsonInput.trim() || validateMutation.isPending}
              >
                {validateMutation.isPending
                  ? t('import_wizard.upload.checking')
                  : t('import_wizard.upload.continue')}
              </Button>
            </div>
          </div>
        ) : null}

        {step === 'review' && validation ? (
          <div className="grid gap-5 p-6 md:grid-cols-2">
            <div className="flex flex-col gap-3">
              <div className="border-border bg-secondary flex items-center gap-3 rounded-[16px] border p-3.5">
                <FileJson className="text-primary size-5 shrink-0" />
                <div className="min-w-0 text-[12.5px]">
                  <div className="font-semibold">{t('import_wizard.review.detected')}</div>
                  <div className="text-muted-foreground">
                    {t('import_wizard.review.detected_meta', {
                      products: products.length,
                      recipes: parsed?.recipes?.length ?? 0,
                      days,
                    })}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <DetectedTile label={t('common.products')} value={products.length} />
                <DetectedTile label={t('common.recipes')} value={parsed?.recipes?.length ?? 0} />
                <DetectedTile label={t('import_wizard.review.days')} value={days} />
                <DetectedTile label={t('import_wizard.review.meal_entries')} value={mealEntries} />
              </div>
            </div>

            <div className="flex flex-col gap-3">
              {validation.isApiError || !canProceed ? (
                <Banner
                  variant="error"
                  title={t('import_wizard.review.cannot_proceed')}
                  retryLabel={t('import_wizard.review.revalidate')}
                  onRetry={() => {
                    if (parsed) void runValidation(parsed);
                  }}
                >
                  <ul className="list-disc space-y-0.5 pl-4">
                    {validation.issues.slice(0, 6).map((issue, i) => (
                      <li key={i}>
                        {issue.item ? `${issue.item}: ` : ''}
                        {issue.message}
                      </li>
                    ))}
                  </ul>
                </Banner>
              ) : warnings > 0 ? (
                <Banner
                  variant="warning"
                  title={t('import_wizard.review.warnings_title', { count: warnings })}
                >
                  <ul className="list-disc space-y-0.5 pl-4">
                    {validation.issues.slice(0, 6).map((issue, i) => (
                      <li key={i}>
                        {issue.item ? `${issue.item}: ` : ''}
                        {issue.message}
                      </li>
                    ))}
                  </ul>
                </Banner>
              ) : (
                <Banner variant="success" title={t('import_wizard.review.ready')}>
                  {t('import_wizard.review.ready_hint', { count: newProducts })}
                </Banner>
              )}

              {products.length > 0 ? (
                <div className="border-border overflow-hidden rounded-[16px] border">
                  <div className="bg-secondary text-muted-foreground grid grid-cols-[1.6fr_1fr_0.8fr] gap-2 px-3 py-2 text-[10.5px] font-semibold uppercase">
                    <span>{t('import_wizard.review.col_product')}</span>
                    <span>{t('import_wizard.review.col_unit')}</span>
                    <span className="text-right">kcal</span>
                  </div>
                  <div className="max-h-[220px] overflow-auto">
                    {products.map((p, i) => {
                      const missing = !p.unit;
                      return (
                        <div
                          key={i}
                          className="border-border grid grid-cols-[1.6fr_1fr_0.8fr] gap-2 border-t px-3 py-2 text-[12px]"
                        >
                          <span className="truncate font-semibold">{p.name}</span>
                          <span className={cn(missing && 'text-destructive font-semibold')}>
                            {missing ? t('import_wizard.review.missing_unit') : p.unit}
                          </span>
                          <span className="tnum text-right">
                            {formatNumber(num(p.caloriesPer100g))}
                          </span>
                        </div>
                      );
                    })}
                  </div>
                </div>
              ) : null}
            </div>

            {importError ? (
              <p
                role="alert"
                className="border-destructive/30 text-destructive rounded-[16px] border px-4 py-3 text-[12.5px] md:col-span-2"
                style={{
                  background: 'color-mix(in oklab, var(--color-fat) 12%, transparent)',
                }}
              >
                {importError}
              </p>
            ) : null}

            <div className="flex flex-wrap justify-between gap-2 md:col-span-2">
              <Button
                variant="outline"
                size="xl"
                onClick={() => {
                  setStep('upload');
                }}
              >
                {t('common.previous')}
              </Button>
              <Button
                size="xl"
                className="flex-1"
                onClick={() => {
                  void handleImport();
                }}
                disabled={!canProceed || importMutation.isPending}
              >
                {importMutation.isPending
                  ? t('import_wizard.review.importing')
                  : t('import_wizard.review.import_days', { count: days })}
              </Button>
            </div>
          </div>
        ) : null}

        {step === 'done' ? (
          <div className="flex flex-col items-center gap-4 px-6 py-16 text-center">
            <div
              className="grid size-14 place-items-center rounded-2xl"
              style={{ background: 'color-mix(in oklab, var(--color-good) 14%, transparent)' }}
            >
              <Check className="size-7 text-[var(--color-good)]" strokeWidth={2.4} />
            </div>
            <h2 className="text-[22px] font-bold">{t('import_wizard.done.title')}</h2>
            <p className="text-muted-foreground text-sm">{t('import_wizard.done.message')}</p>
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function DetectedTile({ label, value }: { label: string; value: number }) {
  return (
    <div className="border-border bg-secondary rounded-[15px] border p-3.5">
      <div className="text-muted-foreground text-[10.5px] font-semibold tracking-[0.05em] uppercase">
        {label}
      </div>
      <div className="numeral mt-0.5 text-[19px] font-bold">{value}</div>
    </div>
  );
}

const STEPS: Step[] = ['upload', 'review', 'done'];

function StepBar({ step }: { step: Step }) {
  const { t } = useTranslation();
  const current = STEPS.indexOf(step);
  const labels = [
    t('import_wizard.stepbar.upload'),
    t('import_wizard.stepbar.review'),
    t('import_wizard.stepbar.confirm'),
  ];

  return (
    <div className="border-border bg-secondary flex items-center gap-2 border-b px-6 py-5">
      {labels.map((label, i) => {
        const state = i < current ? 'done' : i === current ? 'current' : 'upcoming';
        return (
          <div
            key={label}
            className={cn(
              'flex items-center gap-2',
              i < labels.length - 1 && 'flex-1',
              state === 'upcoming' && 'opacity-55',
            )}
          >
            <span
              className={cn(
                'grid size-7 flex-none place-items-center rounded-full text-[12.5px] font-bold',
                state === 'done' && 'text-white',
                state === 'current' && 'bg-primary text-primary-foreground',
                state === 'upcoming' && 'bg-muted border-border-strong border',
              )}
              style={state === 'done' ? { background: 'var(--color-good)' } : undefined}
            >
              {state === 'done' ? <Check className="size-3.5" strokeWidth={3} /> : i + 1}
            </span>
            <span className="text-[13px] font-semibold">{label}</span>
            {i < labels.length - 1 ? (
              <span
                className={cn(
                  'h-0.5 flex-1 rounded-full',
                  state === 'done' ? 'bg-[var(--color-good)]' : 'bg-border-strong',
                )}
              />
            ) : null}
          </div>
        );
      })}
    </div>
  );
}
