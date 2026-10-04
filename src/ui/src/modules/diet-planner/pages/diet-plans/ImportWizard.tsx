import { useValidateImport, useExecuteImport } from '@modules/diet-planner/api/hooks/useMeals';
import { Banner, Button, Card, PageContainer, PageHeader } from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { useFormat } from '@shared/hooks/useFormat';
import { cn } from '@shared/lib/utils';
import { Check, FileJson, TriangleAlert, Upload } from 'lucide-react';
import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import type { components } from '../../api/generated/schema';

type ImportDto = components['schemas']['ImportDto'];
type ValidationResultDto = components['schemas']['ValidationResultDto'];

interface ValidationState extends ValidationResultDto {
  isApiError?: boolean;
}

interface Source {
  name: string;
  bytes: number;
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
const sampleText = JSON.stringify(sampleJson, null, 2);
const SAMPLE_URL = `data:application/json;charset=utf-8,${encodeURIComponent(sampleText)}`;

const byteLength = (text: string) => new Blob([text]).size;

export default function ImportWizard() {
  const { t } = useTranslation();
  const fmt = useFormat();
  const toast = useToast();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [step, setStep] = useState<Step>('upload');
  const [jsonInput, setJsonInput] = useState('');
  const [jsonError, setJsonError] = useState<string | null>(null);
  const [dragging, setDragging] = useState(false);
  const [source, setSource] = useState<Source | null>(null);
  const [parsed, setParsed] = useState<ImportDto | null>(null);
  const [validation, setValidation] = useState<ValidationState | null>(null);
  const [importError, setImportError] = useState<string | null>(null);

  const validateMutation = useValidateImport();
  const importMutation = useExecuteImport();

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

  /** Parse, validate (a dry run) and move to the review step. */
  const check = async (text: string, from: Source) => {
    let data: ImportDto;
    try {
      data = JSON.parse(text) as ImportDto;
    } catch {
      setJsonError(t('import_wizard.upload.invalid_json'));
      return;
    }
    setJsonError(null);
    setSource(from);
    setParsed(data);
    setImportError(null);
    await runValidation(data);
    setStep('review');
  };

  const readFile = (file: File) => {
    if (!file.name.toLowerCase().endsWith('.json')) {
      setJsonError(t('import_wizard.upload.not_json'));
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      const text = typeof reader.result === 'string' ? reader.result : '';
      setJsonInput(text);
      void check(text, { name: file.name, bytes: file.size });
    };
    reader.readAsText(file);
  };

  const startOver = () => {
    setStep('upload');
    setJsonInput('');
    setJsonError(null);
    setSource(null);
    setParsed(null);
    setValidation(null);
    setImportError(null);
  };

  /** Back keeps what was entered; "Choose another file" starts over. */
  const back = () => {
    setStep('upload');
    setSource(null);
    setParsed(null);
    setValidation(null);
    setImportError(null);
  };

  const handleImport = async () => {
    if (!parsed) return;
    setImportError(null);
    try {
      await importMutation.mutateAsync(parsed);
      setStep('done');
    } catch {
      const msg = t('import_wizard.import_error');
      setImportError(msg);
      toast.error(msg);
    }
  };

  const schedule = parsed?.schedule ?? [];
  const days = schedule.length;
  const dates = schedule
    .map((d) => d.date)
    .filter((d): d is string => Boolean(d))
    .sort();
  const first = dates[0];
  const last = dates[dates.length - 1];
  // One day reads "5 Oct", not "5 Oct – 5 Oct".
  const range =
    first === undefined || last === undefined
      ? null
      : first === last
        ? (fmt.dayRange(first, last).split(' – ')[0] ?? null)
        : fmt.dayRange(first, last);
  const plan = validation?.plan ?? null;
  const mealEntries =
    num(plan?.mealEntriesToCreate) || schedule.reduce((sum, d) => sum + (d.meals?.length ?? 0), 0);
  const canProceed = validation?.canProceed === true;
  const warnings = (validation?.issues ?? []).filter((issue) => issue.severity === 'warning');
  const errors = (validation?.issues ?? []).filter((issue) => issue.severity === 'error');

  return (
    <PageContainer width="narrow" className="animate-fade-in">
      <PageHeader
        breadcrumb={[
          { label: t('import_wizard.breadcrumb_plan'), href: '/diet-planner/calendar' },
          { label: t('import_wizard.breadcrumb_import') },
        ]}
        title={t('import_wizard.title')}
        subtitle={t('import_wizard.subtitle')}
      />
      <StepBar step={step} />

      <Card className="p-5 md:p-6">
        {step === 'upload' ? (
          <div className="flex flex-col gap-5">
            <div
              onDragOver={(event) => {
                event.preventDefault();
                setDragging(true);
              }}
              onDragLeave={() => {
                setDragging(false);
              }}
              onDrop={(event) => {
                event.preventDefault();
                setDragging(false);
                const file = event.dataTransfer.files[0];
                if (file) readFile(file);
              }}
              className={cn(
                'border-border-strong bg-secondary rounded-18px flex flex-col items-center gap-3 border border-dashed px-6 py-10 text-center transition-colors',
                dragging && 'border-primary bg-accent',
              )}
            >
              <Upload className="text-muted-foreground size-7" strokeWidth={1.9} />
              <div>
                <p className="font-semibold">{t('import_wizard.upload.dropzone_title')}</p>
                <p className="text-muted-foreground text-sm">
                  {t('import_wizard.upload.dropzone_hint')}
                </p>
              </div>
              <div className="flex flex-wrap justify-center gap-2">
                <Button
                  size="xl"
                  disabled={validateMutation.isPending}
                  onClick={() => fileInputRef.current?.click()}
                >
                  {validateMutation.isPending
                    ? t('import_wizard.upload.checking')
                    : t('import_wizard.upload.choose_file')}
                </Button>
                <Button
                  size="xl"
                  variant="outline"
                  disabled={validateMutation.isPending}
                  onClick={() => {
                    setJsonInput(sampleText);
                    void check(sampleText, {
                      name: t('import_wizard.upload.sample_name'),
                      bytes: byteLength(sampleText),
                    });
                  }}
                >
                  {t('import_wizard.upload.try_sample')}
                </Button>
              </div>
              <input
                ref={fileInputRef}
                type="file"
                aria-label={t('import_wizard.upload.choose_file')}
                accept=".json,application/json"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) readFile(file);
                  e.target.value = '';
                }}
              />
            </div>

            {jsonError ? <Banner variant="error">{jsonError}</Banner> : null}

            <details className="group">
              <summary className="text-primary min-h-11 cursor-pointer py-2 text-sm font-semibold">
                {t('import_wizard.upload.paste_toggle')}
              </summary>
              <div className="mt-2 flex flex-col gap-3">
                <textarea
                  aria-label={t('import_wizard.upload.paste_label')}
                  value={jsonInput}
                  onChange={(e) => {
                    setJsonInput(e.target.value);
                  }}
                  placeholder={t('import_wizard.upload.placeholder')}
                  rows={10}
                  className="border-border bg-muted rounded-16px text-11-5px focus-visible:ring-ring border p-3 font-mono leading-relaxed outline-none focus-visible:ring-2"
                />
                <div className="flex justify-end">
                  <Button
                    onClick={() => {
                      void check(jsonInput, {
                        name: t('import_wizard.upload.pasted_name'),
                        bytes: byteLength(jsonInput),
                      });
                    }}
                    disabled={!jsonInput.trim() || validateMutation.isPending}
                  >
                    {validateMutation.isPending
                      ? t('import_wizard.upload.checking')
                      : t('import_wizard.upload.continue')}
                  </Button>
                </div>
              </div>
            </details>

            <details>
              <summary className="text-primary min-h-11 cursor-pointer py-2 text-sm font-semibold">
                {t('import_wizard.upload.see_format')}
              </summary>
              <div className="mt-2 flex flex-col gap-3">
                <pre className="bg-muted max-h-280px rounded-16px text-11-5px overflow-auto p-3 font-mono leading-relaxed whitespace-pre">
                  {sampleText}
                </pre>
                <a
                  href={SAMPLE_URL}
                  download="sample-plan.json"
                  className="text-primary inline-flex min-h-11 items-center text-sm font-semibold"
                >
                  {t('import_wizard.upload.download_sample')}
                </a>
              </div>
            </details>
          </div>
        ) : null}

        {step === 'review' && validation ? (
          <div className="flex flex-col gap-5">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex min-w-0 items-center gap-2.5">
                <FileJson className="text-muted-foreground size-5 shrink-0" />
                <span className="truncate font-semibold">{source?.name}</span>
                {source ? (
                  <span className="text-text-2 text-sm whitespace-nowrap">
                    · {Math.max(1, Math.round(source.bytes / 1024))} KB
                  </span>
                ) : null}
              </div>
              <Button variant="ghost" onClick={startOver}>
                {t('import_wizard.review.choose_another')}
              </Button>
            </div>

            {plan ? (
              <div className="grid gap-3 sm:grid-cols-3">
                <CountTile
                  label={t('import_wizard.review.meals')}
                  value={String(mealEntries)}
                  detail={
                    days > 0
                      ? t('import_wizard.review.meals_detail', {
                          perDay: Math.max(1, Math.round(mealEntries / days)),
                          range,
                        })
                      : undefined
                  }
                />
                <CountTile
                  label={t('common.recipes')}
                  value={t('import_wizard.review.n_new', { count: num(plan.recipesToCreate) })}
                  detail={t('import_wizard.review.recipes_existing', {
                    count: num(plan.recipesToReuse),
                  })}
                />
                <CountTile
                  label={t('common.products')}
                  value={t('import_wizard.review.n_new', { count: num(plan.productsToCreate) })}
                  detail={t('import_wizard.review.products_matched', {
                    count: num(plan.productsToReuse),
                  })}
                />
              </div>
            ) : null}

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
                  {errors.slice(0, 6).map((issue, i) => (
                    <li key={i}>
                      {issue.item ? `${issue.item}: ` : ''}
                      {issue.message}
                    </li>
                  ))}
                </ul>
              </Banner>
            ) : null}

            {warnings.length > 0 ? (
              <section aria-labelledby="import-warnings">
                <h2 id="import-warnings" className="mb-2 font-semibold">
                  {t('import_wizard.review.warnings_title', { count: warnings.length })}
                </h2>
                <ul className="flex flex-col gap-2">
                  {warnings.slice(0, 8).map((issue, i) => (
                    <li
                      key={i}
                      className="bg-warning-soft flex items-start gap-3 rounded-xl px-3.5 py-3 text-sm"
                    >
                      <TriangleAlert className="text-warning mt-0.5 size-4 shrink-0" />
                      <span className="min-w-0 break-words">
                        {issue.item ? <strong>{issue.item}: </strong> : null}
                        {issue.message}
                      </span>
                    </li>
                  ))}
                </ul>
              </section>
            ) : null}

            {importError ? (
              <p
                role="alert"
                className="border-destructive/30 text-destructive rounded-16px text-12-5px border px-4 py-3"
                style={{
                  background: 'color-mix(in oklab, var(--color-fat) 12%, transparent)',
                }}
              >
                {importError}
              </p>
            ) : null}

            <div className="border-border flex flex-wrap items-center justify-between gap-3 border-t pt-4">
              <p className="text-text-2 min-w-0 flex-1 basis-60 text-sm">
                {range
                  ? t('import_wizard.review.existing_kept_range', { range })
                  : t('import_wizard.review.existing_kept')}
              </p>
              <div className="flex gap-2">
                <Button variant="outline" size="xl" onClick={back}>
                  {t('import_wizard.review.back')}
                </Button>
                <Button
                  size="xl"
                  onClick={() => {
                    void handleImport();
                  }}
                  disabled={!canProceed || importMutation.isPending}
                >
                  {importMutation.isPending
                    ? t('import_wizard.review.importing')
                    : t('import_wizard.review.import_meals', { count: mealEntries })}
                </Button>
              </div>
            </div>
          </div>
        ) : null}

        {step === 'done' ? (
          <div className="flex flex-col items-center gap-4 px-6 py-10 text-center">
            <div
              className="grid size-14 place-items-center rounded-2xl"
              style={{ background: 'color-mix(in oklab, var(--color-good) 14%, transparent)' }}
            >
              <Check className="text-good size-7" strokeWidth={2.4} />
            </div>
            <h2 className="text-22px font-bold">{t('import_wizard.done.title')}</h2>
            <p className="text-muted-foreground text-sm">
              {t('import_wizard.done.message', { count: mealEntries })}
            </p>
            <div className="flex flex-wrap justify-center gap-2">
              <Button variant="outline" size="xl" onClick={startOver}>
                {t('import_wizard.done.another')}
              </Button>
              <Button size="xl" asChild>
                <Link to="/diet-planner/calendar">{t('import_wizard.done.open_plan')}</Link>
              </Button>
            </div>
          </div>
        ) : null}
      </Card>
    </PageContainer>
  );
}

function CountTile({
  label,
  value,
  detail,
}: {
  label: string;
  value: string;
  detail?: string | undefined;
}) {
  return (
    <div className="border-border rounded-xl border p-3.5">
      <div className="text-muted-foreground text-label">{label}</div>
      <div className="numeral text-2xl leading-tight font-bold">{value}</div>
      {detail ? <div className="text-text-2 text-sm">{detail}</div> : null}
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
    <ol aria-label={t('import_wizard.title')} className="mb-4 flex flex-wrap items-center gap-2">
      {labels.map((label, i) => {
        const state = i < current ? 'done' : i === current ? 'current' : 'upcoming';
        return (
          <li
            key={label}
            aria-current={state === 'current' ? 'step' : undefined}
            className={cn(
              'flex items-center gap-2 rounded-full py-1 pr-3 pl-1',
              state === 'current' && 'bg-accent',
              state === 'upcoming' && 'text-text-2',
            )}
          >
            <span
              className={cn(
                'text-12-5px grid size-6 flex-none place-items-center rounded-full font-bold',
                state === 'done' && 'bg-primary text-primary-foreground',
                state === 'current' && 'bg-primary text-primary-foreground',
                state === 'upcoming' && 'bg-muted border-border-strong border',
              )}
            >
              {state === 'done' ? <Check className="size-3.5" strokeWidth={3} /> : i + 1}
            </span>
            <span
              className={cn(
                'text-sm font-semibold',
                state === 'current' && 'text-accent-foreground',
              )}
            >
              {label}
            </span>
          </li>
        );
      })}
    </ol>
  );
}
