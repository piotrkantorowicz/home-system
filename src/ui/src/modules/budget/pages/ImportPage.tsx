import { useHousehold } from '@modules/household';
import {
  Banner,
  Button,
  EmptyState,
  Field,
  Input,
  PageContainer,
  PageHeader,
  Select,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { FileUp } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { errorProblem, errorStatus } from '../api/client';
import { useAccountsQuery, useImportMutation, useStatementPreviewMutation } from '../api/queries';
import { ImportReviewList } from '../components/ImportReviewList';
import { useRequestId } from '../hooks/useRequestId';
import { importRows, isSelectable, reviewRows, rowErrors } from '../lib/imports';

import type { ImportParties, ReviewRow } from '../lib/imports';
import type { ImportRowInput, StatementPreview } from '../types';

type Step =
  | { kind: 'upload' }
  | { kind: 'review'; preview: StatementPreview; rows: ReviewRow[] }
  | { kind: 'done'; count: number };

type Failure = 'preview' | 'unreadable' | 'conflict' | 'rows' | 'import' | null;

const MAX_FILE_BYTES = 2 * 1024 * 1024;

export default function ImportPage() {
  const { t } = useTranslation('budget');
  const { dayShort } = useFormat();
  const { members, myPersonId } = useHousehold();
  const accounts = useAccountsQuery();
  const preview = useStatementPreviewMutation();
  const importMutation = useImportMutation();
  const requestId = useRequestId();

  const envelopes = (accounts.data ?? []).filter((a) => !a.isArchived);
  const adults = members.filter((m) => m.role === 'Owner' || m.role === 'Adult');
  const [accountId, setAccountId] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [funding, setFunding] = useState<'Individual' | 'HouseholdFunds'>('Individual');
  const [payer, setPayer] = useState(myPersonId ?? '');
  const [step, setStep] = useState<Step>({ kind: 'upload' });
  const [failure, setFailure] = useState<Failure>(null);
  const [errors, setErrors] = useState<Map<string, string[]>>(new Map());

  const chosenAccount =
    envelopes.find((a) => a.id === accountId) ??
    envelopes.find((a) => a.visibility === 'Household') ??
    envelopes[0];
  const personal = chosenAccount?.visibility === 'Personal';
  const parties: ImportParties | null = personal
    ? null
    : {
        fundingSource: funding,
        paidByPersonId: payer || (adults[0]?.personId ?? ''),
        participantIds: adults.map((m) => m.personId),
      };
  const tooLarge = file !== null && file.size > MAX_FILE_BYTES;

  const handlePreview = (previous: ReviewRow[] = []) => {
    if (!file || !chosenAccount || tooLarge) return;
    setFailure(null);
    setErrors(new Map());
    preview.mutate(
      { accountId: chosenAccount.id, file },
      {
        onSuccess: (data) => {
          setStep({ kind: 'review', preview: data, rows: reviewRows(data.candidates, previous) });
        },
        onError: (error) => {
          setFailure(errorStatus(error) === 422 ? 'unreadable' : 'preview');
        },
      },
    );
  };

  // A rejected import writes nothing; the review step stays as it was, edits included.
  const handleImport = (payload: ImportRowInput[]) => {
    if (!chosenAccount || payload.length === 0) return;
    setFailure(null);
    setErrors(new Map());
    importMutation.mutate(
      {
        requestId: requestId.idFor(JSON.stringify([chosenAccount.id, payload])),
        accountId: chosenAccount.id,
        rows: payload,
      },
      {
        onSuccess: (result) => {
          requestId.reset();
          setStep({ kind: 'done', count: result.expenseIds.length });
        },
        onError: (error) => {
          const status = errorStatus(error);
          const perRow = rowErrors(errorProblem(error), payload);
          if (status === 409) setFailure('conflict');
          else if (perRow.size > 0) {
            setErrors(perRow);
            setFailure('rows');
          } else setFailure('import');
        },
      },
    );
  };

  const updateRow = (sourceKey: string, change: Partial<ReviewRow>) => {
    if (step.kind !== 'review') return;
    setStep({
      ...step,
      rows: step.rows.map((r) => (r.candidate.sourceKey === sourceKey ? { ...r, ...change } : r)),
    });
  };

  return (
    <PageContainer width="form">
      <PageHeader
        title={t('import_title')}
        subtitle={t('import_subtitle')}
        breadcrumb={[
          { label: t('expenses'), href: '/budget/expenses' },
          { label: t('import_title') },
        ]}
      />

      {step.kind === 'done' ? (
        <div className="space-y-4">
          <EmptyState
            icon={FileUp}
            title={t('import_done_title', { count: step.count })}
            description={t('import_done_body')}
            action={{ label: t('import_view_expenses'), href: '/budget/expenses' }}
          />
          <div className="flex justify-center">
            <Button
              variant="outline"
              onClick={() => {
                setFile(null);
                setStep({ kind: 'upload' });
              }}
            >
              {t('import_another')}
            </Button>
          </div>
        </div>
      ) : (
        <>
          <form
            className="mb-6 grid gap-4 sm:grid-cols-2"
            aria-label={t('import_title')}
            onSubmit={(event) => {
              event.preventDefault();
              handlePreview();
            }}
          >
            <Field id="import-envelope" label={t('envelope')}>
              <Select
                id="import-envelope"
                value={chosenAccount?.id ?? ''}
                onChange={(event) => {
                  setAccountId(event.target.value);
                  setStep({ kind: 'upload' });
                }}
              >
                {envelopes.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.name}
                  </option>
                ))}
              </Select>
            </Field>
            <Field
              id="import-file"
              label={t('import_file')}
              hint={t('import_file_hint')}
              error={tooLarge ? t('import_file_too_large') : undefined}
            >
              <Input
                id="import-file"
                type="file"
                accept=".csv,text/csv"
                onChange={(event) => {
                  setFile(event.target.files?.[0] ?? null);
                  setStep({ kind: 'upload' });
                }}
              />
            </Field>
            {!personal && (
              <>
                <Field id="import-funding" label={t('import_funding')}>
                  <Select
                    id="import-funding"
                    value={funding}
                    onChange={(event) => {
                      setFunding(event.target.value as 'Individual' | 'HouseholdFunds');
                    }}
                  >
                    <option value="Individual">{t('import_funding_individual')}</option>
                    <option value="HouseholdFunds">{t('import_funding_household')}</option>
                  </Select>
                </Field>
                {funding === 'Individual' && (
                  <Field id="import-payer" label={t('import_payer')} hint={t('import_split_hint')}>
                    <Select
                      id="import-payer"
                      value={parties?.paidByPersonId ?? ''}
                      onChange={(event) => {
                        setPayer(event.target.value);
                      }}
                    >
                      {adults.map((m) => (
                        <option key={m.personId} value={m.personId}>
                          {m.personId === myPersonId ? t('you') : m.displayName}
                        </option>
                      ))}
                    </Select>
                  </Field>
                )}
              </>
            )}
            <div className="sm:col-span-2">
              <Button
                type="submit"
                variant={step.kind === 'review' ? 'outline' : 'default'}
                disabled={!file || !chosenAccount || tooLarge || preview.isPending}
              >
                {t(step.kind === 'review' ? 'import_preview_again' : 'import_preview')}
              </Button>
            </div>
          </form>

          {failure === 'preview' && <Banner variant="error">{t('import_preview_error')}</Banner>}
          {failure === 'unreadable' && <Banner variant="error">{t('import_unreadable')}</Banner>}

          {step.kind === 'review' && (
            <Review
              step={step}
              failure={failure}
              errors={errors}
              parties={parties}
              pending={importMutation.isPending || preview.isPending}
              dayShort={dayShort}
              onChange={updateRow}
              onImport={handleImport}
              onPreviewAgain={() => {
                handlePreview(step.rows);
              }}
            />
          )}
        </>
      )}
    </PageContainer>
  );
}

interface ReviewProps {
  step: Extract<Step, { kind: 'review' }>;
  failure: Failure;
  errors: Map<string, string[]>;
  parties: ImportParties | null;
  pending: boolean;
  dayShort: (day: string) => string;
  onChange: (sourceKey: string, change: Partial<ReviewRow>) => void;
  onImport: (payload: ImportRowInput[]) => void;
  onPreviewAgain: () => void;
}

function Review({
  step,
  failure,
  errors,
  parties,
  pending,
  dayShort,
  onChange,
  onImport,
  onPreviewAgain,
}: ReviewProps) {
  const { t } = useTranslation('budget');
  const { preview, rows } = step;
  const payload = importRows(rows, parties);
  const selectable = rows.filter((r) => isSelectable(r.candidate)).length;

  return (
    <section aria-label={t('import_review')} className="space-y-4">
      <p className="text-text-2 text-meta">
        {t('import_summary', {
          profile: preview.profile,
          from: preview.periodFrom ? dayShort(preview.periodFrom) : '—',
          to: preview.periodTo ? dayShort(preview.periodTo) : '—',
          count: preview.candidates.length,
          selectable,
        })}
      </p>

      {failure === 'conflict' && (
        <Banner variant="warning" onRetry={onPreviewAgain} retryLabel={t('import_preview_again')}>
          {t('import_conflict')}
        </Banner>
      )}
      {failure === 'rows' && <Banner variant="error">{t('import_rows_rejected')}</Banner>}
      {failure === 'import' && <Banner variant="error">{t('import_error')}</Banner>}
      {preview.issues.length > 0 && (
        <Banner variant="warning">
          {t('import_issues', {
            lines: preview.issues.map((i) => String(i.lineNumber)).join(', '),
            count: preview.issues.length,
          })}
        </Banner>
      )}

      {rows.length === 0 ? (
        <p>{t('import_no_candidates')}</p>
      ) : (
        <ImportReviewList rows={rows} errors={errors} onChange={onChange} />
      )}

      {preview.skipped.length > 0 && (
        <details>
          <summary className="text-text-2 text-meta cursor-pointer">
            {t('import_skipped', { count: preview.skipped.length })}
          </summary>
          <ul className="text-text-2 text-meta mt-2 space-y-1">
            {preview.skipped.map((s) => (
              <li key={String(s.lineNumber)}>
                {dayShort(s.occurredOn)} · {s.description} · {s.amount} {s.currency} —{' '}
                {t(`import_skip_reason.${s.reason}`)}
              </li>
            ))}
          </ul>
        </details>
      )}

      <div className="flex justify-end">
        <Button
          disabled={payload.length === 0 || pending}
          onClick={() => {
            onImport(payload);
          }}
        >
          {t('import_confirm', { count: payload.length })}
        </Button>
      </div>
    </section>
  );
}
