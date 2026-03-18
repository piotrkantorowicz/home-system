import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { FileJson, AlertCircle, CheckCircle2, ArrowRight, ArrowLeft, XCircle } from 'lucide-react';
import { useValidateImport, useExecuteImport } from '@modules/diet-planner/api/hooks/useMeals';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Textarea,
  Label,
  Table,
  TableHeader,
  TableBody,
  TableHead,
  TableRow,
  TableCell,
} from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import type { components } from '../../api/generated/schema';

type ImportDto = components['schemas']['ImportDto'];
type ValidationResultDto = components['schemas']['ValidationResultDto'];
type ValidationIssueDto = components['schemas']['ValidationIssueDto'];

interface ValidationState extends ValidationResultDto {
  isApiError?: boolean;
}

export default function ImportWizard() {
  const { t } = useTranslation();
  const [step, setStep] = useState<1 | 2 | 3 | 4>(1);
  const [jsonInput, setJsonInput] = useState('');
  const [importData, setImportData] = useState<ImportDto | null>(null);
  const [validationResult, setValidationResult] = useState<ValidationState | null>(null);

  const navigate = useNavigate();
  const validateMutation = useValidateImport();
  const importMutation = useExecuteImport();

  const handleJsonParse = () => {
    try {
      const parsed = JSON.parse(jsonInput);
      setImportData(parsed);
      setStep(2);
    } catch {
      alert(t('import_wizard.step1.invalid_json'));
    }
  };

  const handleValidate = async () => {
    if (!importData) return;

    try {
      const result = await validateMutation.mutateAsync(importData);
      const validationData = { ...(result as ValidationResultDto), isApiError: false };
      setValidationResult(validationData);

      if (validationData.canProceed) {
        setStep(3);
      }
    } catch (error) {
      console.error('Validation API error:', error);
      const errorMessage = error instanceof Error ? error.message : 'An unexpected error occurred';
      setValidationResult({
        valid: false,
        canProceed: false,
        summary: { errors: 1, warnings: 0, info: 0 },
        issues: [{ severity: 'error', category: 'api', message: errorMessage }],
        plan: null,
        isApiError: true,
      });
    }
  };

  const hasDetailedIssues = (issues: ValidationIssueDto[] | undefined): boolean => {
    if (!issues || issues.length === 0) return false;
    return issues.some((issue) => issue.category || issue.path || issue.item);
  };

  const handleImport = async () => {
    if (!importData) return;

    try {
      await importMutation.mutateAsync(importData);
      setStep(4);
      setTimeout(() => {
        navigate('/diet-planner/calendar');
      }, 2000);
    } catch (error) {
      console.error('Import failed:', error);
    }
  };

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
        meals: [
          { type: 'lunch', recipe: 'Grilled Chicken with Rice', servings: 1 },
        ],
      },
      {
        date: '2026-03-19',
        meals: [
          { type: 'dinner', recipe: 'Grilled Chicken with Rice', servings: 1 },
        ],
      },
    ],
  };

  return (
    <div className="p-8 lg:p-10 max-w-4xl mx-auto animate-fade-in-up">
      <div className="mb-8">
        <h1 className="text-4xl font-bold tracking-tight mb-2">{t('import_wizard.title')}</h1>
        <p className="text-muted-foreground text-[0.95rem]">{t('import_wizard.subtitle')}</p>
      </div>

      {/* Progress Steps */}
      <div className="mb-10 flex items-center justify-between">
        {[
          { num: 1, label: t('import_wizard.steps.upload') },
          { num: 2, label: t('import_wizard.steps.validate') },
          { num: 3, label: t('import_wizard.steps.review') },
          { num: 4, label: t('import_wizard.steps.complete') },
        ].map((s, idx) => (
          <div key={s.num} className="flex items-center">
            <div
              className={cn(
                'flex h-10 w-10 items-center justify-center rounded-xl border-2 text-sm font-bold transition-all duration-300',
                step >= s.num
                  ? 'border-primary bg-primary/10 text-primary'
                  : 'border-muted bg-background text-muted-foreground'
              )}
            >
              {step > s.num ? <CheckCircle2 className="h-5 w-5" /> : s.num}
            </div>
            <span
              className={cn(
                'ml-2 text-sm font-medium transition-colors',
                step >= s.num ? 'text-foreground' : 'text-muted-foreground'
              )}
            >
              {s.label}
            </span>
            {idx < 3 && <ArrowRight className="mx-4 h-4 w-4 text-muted-foreground/40" />}
          </div>
        ))}
      </div>

      {/* Step 1: Upload/Paste JSON */}
      {step === 1 && (
        <Card className="animate-scale-in">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileJson className="h-5 w-5 text-primary" />
              {t('import_wizard.step1.title')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            <div>
              <Label htmlFor="json-input">{t('import_wizard.step1.label')}</Label>
              <Textarea
                id="json-input"
                value={jsonInput}
                onChange={(e) => setJsonInput(e.target.value)}
                placeholder={t('import_wizard.step1.placeholder')}
                rows={12}
                className="font-mono text-sm"
              />
            </div>

            <div className="flex gap-4">
              <Button onClick={handleJsonParse} disabled={!jsonInput.trim()}>
                {t('import_wizard.step1.continue')}
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
              <Button
                variant="outline"
                onClick={() => setJsonInput(JSON.stringify(sampleJson, null, 2))}
              >
                {t('import_wizard.step1.load_sample')}
              </Button>
            </div>

            <div className="rounded-xl border border-muted bg-muted/30 p-5">
              <p className="text-sm font-semibold mb-2">
                {t('import_wizard.step1.expected_format')}
              </p>
              <pre className="text-xs overflow-auto max-h-32 text-muted-foreground">
                {JSON.stringify(sampleJson, null, 2)}
              </pre>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Step 2: Validation (Dry Run) */}
      {step === 2 && importData && (
        <Card className="animate-scale-in">
          <CardHeader>
            <CardTitle>{t('import_wizard.step2.title')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid grid-cols-2 md:grid-cols-3 gap-4 p-5 bg-muted/20 rounded-xl">
              <div>
                <span className="text-xs text-muted-foreground block uppercase tracking-wider mb-1">
                  {t('import_wizard.step2.products')}
                </span>
                <span className="font-medium text-sm block">
                  {importData.products?.length || 0} items
                </span>
              </div>
              <div>
                <span className="text-xs text-muted-foreground block uppercase tracking-wider mb-1">
                  {t('import_wizard.step2.recipes')}
                </span>
                <span className="font-medium text-sm block">
                  {importData.recipes?.length || 0} items
                </span>
              </div>
              <div>
                <span className="text-xs text-muted-foreground block uppercase tracking-wider mb-1">
                  {t('import_wizard.step2.schedule_days')}
                </span>
                <span className="font-medium text-sm block">
                  {importData.schedule?.length || 0} days
                </span>
              </div>
            </div>

            {validationResult && !validationResult.canProceed && (
              <div className="space-y-4 animate-fade-in-up">
                {validationResult.isApiError ? (
                  <div className="rounded-xl border border-red-300 bg-red-50 dark:border-red-800 dark:bg-red-950/20 p-6">
                    <div className="flex items-start gap-4">
                      <div className="rounded-xl bg-red-100 dark:bg-red-900/50 p-3">
                        <XCircle className="h-6 w-6 text-red-600 dark:text-red-400" />
                      </div>
                      <div className="flex-1">
                        <h3 className="text-lg font-semibold text-red-700 dark:text-red-400 mb-2">
                          Something went wrong
                        </h3>
                        <p className="text-red-600 dark:text-red-300 text-sm mb-3">
                          {validationResult.issues?.[0]?.message ||
                            'An unexpected error occurred while validating your import data.'}
                        </p>
                        <p className="text-red-500 dark:text-red-400 text-xs">
                          Please check your data and try again. If the problem persists, contact
                          support.
                        </p>
                      </div>
                    </div>
                  </div>
                ) : (
                  <>
                    <div className="rounded-xl border border-red-200 bg-red-50 dark:border-red-900 dark:bg-red-900/10 p-4">
                      <div className="flex items-center gap-2">
                        <XCircle className="h-5 w-5 text-red-600 dark:text-red-400" />
                        <h3 className="font-semibold text-red-700 dark:text-red-400">
                          Validation Failed
                        </h3>
                        {validationResult.summary && (
                          <span className="ml-auto text-sm font-medium text-red-600 dark:text-red-400">
                            {validationResult.summary.errors} Error
                            {Number(validationResult.summary.errors) !== 1 ? 's' : ''}
                          </span>
                        )}
                      </div>
                    </div>

                    {validationResult.issues &&
                      validationResult.issues.length > 0 &&
                      hasDetailedIssues(validationResult.issues) && (
                        <div className="rounded-xl border overflow-hidden">
                          <Table>
                            <TableHeader>
                              <TableRow className="bg-muted/30">
                                <TableHead className="w-[100px]">Severity</TableHead>
                                <TableHead className="w-[100px]">Type</TableHead>
                                <TableHead className="w-[120px]">Item</TableHead>
                                <TableHead>Message</TableHead>
                                <TableHead className="w-[200px]">Existing Item</TableHead>
                              </TableRow>
                            </TableHeader>
                            <TableBody>
                              {validationResult.issues.map(
                                (issue: ValidationIssueDto, idx: number) => {
                                  const isError = issue.severity === 'error';
                                  const existingItem = issue.existingItem as
                                    | Record<string, unknown>
                                    | undefined;
                                  return (
                                    <TableRow
                                      key={idx}
                                      className={
                                        isError
                                          ? 'bg-red-50/50 dark:bg-red-950/10'
                                          : 'bg-amber-50/50 dark:bg-amber-950/10'
                                      }
                                    >
                                      <TableCell>
                                        <span
                                          className={cn(
                                            'inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium',
                                            isError
                                              ? 'bg-red-100 text-red-700 dark:bg-red-900/50 dark:text-red-400'
                                              : 'bg-amber-100 text-amber-700 dark:bg-amber-900/50 dark:text-amber-400'
                                          )}
                                        >
                                          {issue.severity}
                                        </span>
                                      </TableCell>
                                      <TableCell>
                                        <div className="flex flex-col gap-1">
                                          <span className="text-xs font-medium">
                                            {issue.category || '-'}
                                          </span>
                                          {issue.path && (
                                            <span className="text-xs text-muted-foreground font-mono">
                                              {issue.path}
                                            </span>
                                          )}
                                        </div>
                                      </TableCell>
                                      <TableCell className="font-medium">
                                        {issue.item || '-'}
                                      </TableCell>
                                      <TableCell>
                                        <div className="space-y-1">
                                          <p className="text-sm">{issue.message}</p>
                                          {issue.resolution && (
                                            <p className="text-xs text-blue-600 dark:text-blue-400">
                                              <span className="font-medium">Tip:</span>{' '}
                                              {issue.resolution}
                                            </p>
                                          )}
                                        </div>
                                      </TableCell>
                                      <TableCell>
                                        {existingItem ? (
                                          <div className="text-xs space-y-0.5">
                                            {Object.entries(existingItem)
                                              .filter(([key]) => key !== 'id')
                                              .map(([key, val]) => (
                                                <div
                                                  key={key}
                                                  className="flex justify-between gap-2"
                                                >
                                                  <span className="text-muted-foreground">
                                                    {key.replace(/([A-Z])/g, ' $1').trim()}:
                                                  </span>
                                                  <span className="font-mono">{String(val)}</span>
                                                </div>
                                              ))}
                                          </div>
                                        ) : (
                                          <span className="text-muted-foreground">-</span>
                                        )}
                                      </TableCell>
                                    </TableRow>
                                  );
                                }
                              )}
                            </TableBody>
                          </Table>
                        </div>
                      )}
                  </>
                )}
              </div>
            )}

            <div className="flex gap-4 pt-4 border-t">
              <Button onClick={() => setStep(1)} variant="outline">
                <ArrowLeft className="mr-2 h-4 w-4" />
                {t('common.previous')}
              </Button>
              <Button onClick={handleValidate} disabled={validateMutation.isPending}>
                {validateMutation.isPending
                  ? t('import_wizard.step2.validating')
                  : validationResult && !validationResult.canProceed
                    ? t('import_wizard.step2.revalidate', 'Re-validate')
                    : t('import_wizard.step2.validate')}
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Step 3: Review & Confirm */}
      {step === 3 && validationResult && validationResult.canProceed && (
        <Card className="animate-scale-in">
          <CardHeader>
            <CardTitle>{t('import_wizard.step3.title')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            {validationResult.valid ? (
              <div className="rounded-xl border border-green-200 bg-green-50 dark:border-green-900 dark:bg-green-900/10 p-4">
                <div className="flex items-center gap-2 text-green-700 dark:text-green-400">
                  <CheckCircle2 className="h-5 w-5" />
                  <span className="font-medium">{t('import_wizard.step3.success')}</span>
                </div>
              </div>
            ) : (
              <div className="rounded-xl border border-amber-200 bg-amber-50 dark:border-amber-900 dark:bg-amber-900/10 p-4">
                <div className="flex items-center gap-2 text-amber-700 dark:text-amber-400">
                  <AlertCircle className="h-5 w-5" />
                  <span className="font-medium">
                    {t('import_wizard.step3.warnings_exist', 'Validation passed with warnings')}
                  </span>
                  {validationResult.summary && (
                    <span className="ml-auto text-sm">
                      {validationResult.summary.warnings} Warning
                      {Number(validationResult.summary.warnings) !== 1 ? 's' : ''}
                    </span>
                  )}
                </div>
                {validationResult.issues && validationResult.issues.length > 0 && (
                  <ul className="mt-2 text-sm space-y-1 pl-7">
                    {validationResult.issues.map((issue, idx) => (
                      <li key={idx} className="text-amber-600 dark:text-amber-300">
                        • {issue.item ? `${issue.item}: ` : ''}
                        {issue.message}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            )}

            <div className="space-y-3">
              <h4 className="font-semibold">{t('import_wizard.step3.what_imported')}</h4>
              <div className="rounded-xl border p-5 grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-4">
                {[
                  {
                    val: validationResult.plan?.productsToCreate || 0,
                    label: t('import_wizard.step3.new_products'),
                    primary: true,
                  },
                  {
                    val: validationResult.plan?.productsToReuse || 0,
                    label: 'Products Reuse',
                    primary: false,
                  },
                  {
                    val: validationResult.plan?.recipesToCreate || 0,
                    label: t('import_wizard.step3.new_recipes'),
                    primary: true,
                  },
                  {
                    val: validationResult.plan?.recipesToReuse || 0,
                    label: 'Recipes Reuse',
                    primary: false,
                  },
                  {
                    val: validationResult.plan?.mealEntriesToCreate || 0,
                    label: t('import_wizard.step3.meal_entries'),
                    primary: true,
                  },
                ].map((item, idx) => (
                  <div key={idx} className="text-center p-3 bg-muted/30 rounded-xl">
                    <span
                      className={cn(
                        'block text-2xl font-bold',
                        item.primary ? 'text-primary' : 'text-muted-foreground'
                      )}
                    >
                      {item.val}
                    </span>
                    <span className="text-xs text-muted-foreground">{item.label}</span>
                  </div>
                ))}
              </div>
            </div>

            <div className="rounded-xl border border-amber-200 bg-amber-50 dark:border-amber-900 dark:bg-amber-900/10 p-4">
              <div className="flex items-start gap-2 text-amber-700 dark:text-amber-400">
                <AlertCircle className="h-5 w-5 mt-0.5 shrink-0" />
                <div className="text-sm">
                  <p className="font-medium mb-1">{t('import_wizard.step3.warning_title')}</p>
                  <ul className="list-disc list-inside space-y-1">
                    {Number(validationResult.plan?.productsToCreate ?? 0) > 0 && (
                      <li>
                        {t('import_wizard.step3.warning_products', {
                          count: Number(validationResult.plan?.productsToCreate),
                        })}
                      </li>
                    )}
                    {Number(validationResult.plan?.recipesToCreate ?? 0) > 0 && (
                      <li>
                        {t('import_wizard.step3.warning_recipes', {
                          count: Number(validationResult.plan?.recipesToCreate),
                        })}
                      </li>
                    )}
                    <li>{t('import_wizard.step3.warning_undone')}</li>
                  </ul>
                </div>
              </div>
            </div>

            <div className="flex gap-4 pt-2">
              <Button onClick={() => setStep(2)} variant="outline">
                <ArrowLeft className="mr-2 h-4 w-4" />
                {t('common.previous')}
              </Button>
              <Button onClick={handleImport} disabled={importMutation.isPending}>
                {importMutation.isPending
                  ? t('import_wizard.step3.importing')
                  : t('import_wizard.step3.confirm')}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Step 4: Success */}
      {step === 4 && (
        <Card className="animate-scale-in">
          <CardContent className="py-16">
            <div className="text-center space-y-5">
              <div className="flex justify-center">
                <div className="rounded-2xl bg-green-500/10 p-4 animate-float">
                  <CheckCircle2 className="h-14 w-14 text-green-500" />
                </div>
              </div>
              <h2 className="text-3xl font-bold">{t('import_wizard.step4.success_title')}</h2>
              <p className="text-muted-foreground text-lg">
                {t('import_wizard.step4.success_message')}
              </p>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
