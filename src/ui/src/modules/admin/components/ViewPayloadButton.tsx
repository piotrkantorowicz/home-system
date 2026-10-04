import {
  Banner,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@shared/components/ui';
import { useQuery, type QueryKey, type UseQueryOptions } from '@tanstack/react-query';
import { FileJson } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { PayloadView } from '../api/hooks/useDeliveryDeadLetters';

interface ViewPayloadButtonProps<TKey extends QueryKey> {
  options: UseQueryOptions<PayloadView, Error, PayloadView, TKey>;
  /** The full failure text; the list shows only one truncated line of it. */
  error?: string | null | undefined;
}

function pretty(json: string): string {
  try {
    return JSON.stringify(JSON.parse(json), null, 2);
  } catch {
    return json;
  }
}

/**
 * Mounted only while the dialog is open: closing it unmounts the query observer, so gcTime 0
 * drops the payload from the cache instead of keeping it for the row's lifetime.
 */
function PayloadBody<TKey extends QueryKey>({ options, error }: ViewPayloadButtonProps<TKey>) {
  const { t } = useTranslation('admin');
  const { data, isError } = useQuery(options);

  return (
    <>
      <DialogHeader>
        <DialogTitle className="break-all">{data?.heading ?? t('payload_title')}</DialogTitle>
        <DialogDescription>{t('payload_notice')}</DialogDescription>
      </DialogHeader>
      {error ? (
        <section>
          <h3 className="text-label mb-1 font-semibold">{t('last_error')}</h3>
          <pre className="bg-muted max-h-48 overflow-auto rounded-lg p-3 text-xs break-words whitespace-pre-wrap">
            {error}
          </pre>
        </section>
      ) : null}
      {isError && <Banner variant="error">{t('payload_error')}</Banner>}
      {data?.text && <p className="text-sm whitespace-pre-wrap">{data.text}</p>}
      {data && (
        <pre className="bg-muted max-h-96 overflow-auto rounded-lg p-3 text-xs">
          {pretty(data.json)}
        </pre>
      )}
    </>
  );
}

/** Opens a dialog with a dead letter's content; the request only goes out when it opens. */
export function ViewPayloadButton<TKey extends QueryKey>({
  options,
  error,
}: ViewPayloadButtonProps<TKey>) {
  const { t } = useTranslation('admin');
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button
        type="button"
        variant="ghost"
        size="sm"
        onClick={() => {
          setOpen(true);
        }}
      >
        <FileJson className="size-3.5" aria-hidden />
        {t('view')}
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-2xl">
          {open && <PayloadBody options={options} error={error} />}
        </DialogContent>
      </Dialog>
    </>
  );
}
