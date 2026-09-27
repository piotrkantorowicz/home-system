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
}

function pretty(json: string): string {
  try {
    return JSON.stringify(JSON.parse(json), null, 2);
  } catch {
    return json;
  }
}

/** Opens a dialog with a dead letter's content; the request only goes out when it opens. */
export function ViewPayloadButton<TKey extends QueryKey>({
  options,
}: ViewPayloadButtonProps<TKey>) {
  const { t } = useTranslation('admin');
  const [open, setOpen] = useState(false);
  const { data, isError } = useQuery({ ...options, enabled: open });

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
          <DialogHeader>
            <DialogTitle className="break-all">{data?.heading ?? t('payload_title')}</DialogTitle>
            <DialogDescription>{t('payload_notice')}</DialogDescription>
          </DialogHeader>
          {isError && <Banner variant="error">{t('payload_error')}</Banner>}
          {data?.text && <p className="text-sm whitespace-pre-wrap">{data.text}</p>}
          {data && (
            <pre className="bg-muted max-h-96 overflow-auto rounded-lg p-3 text-xs">
              {pretty(data.json)}
            </pre>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}
