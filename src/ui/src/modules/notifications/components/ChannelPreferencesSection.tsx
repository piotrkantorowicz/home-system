import { Banner } from '@shared/components/ui';
import { Mail, Wifi } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { useChannelPreferences } from '../api/hooks/useChannelPreferences';
import { useUpdateChannelPreferences } from '../api/hooks/useUpdateChannelPreferences';
import { ChannelToggleRow } from '../components/ChannelToggleRow';

import type { ChannelPreferencesDto } from '../api/hooks/useChannelPreferences';

export function ChannelPreferencesSection() {
  const { t } = useTranslation('notifications');
  const { data, isLoading, isError, refetch } = useChannelPreferences();
  const update = useUpdateChannelPreferences();
  const saveErrorMsg = update.isError ? t('preferences.save_failed') : null;

  function handleChange(field: keyof ChannelPreferencesDto, next: boolean) {
    if (!data) return;
    const updated: ChannelPreferencesDto = { ...data, [field]: next };
    update.mutate(updated);
  }

  return (
    <div>
      {isLoading && (
        <ul aria-busy="true" aria-label={t('preferences.loading')} className="space-y-3">
          {Array.from({ length: 2 }).map((_, i) => (
            <li key={i} className="bg-muted rounded-13px h-20 animate-pulse" />
          ))}
        </ul>
      )}

      {isError && (
        <Banner variant="error" onRetry={() => void refetch()} retryLabel={t('preferences.retry')}>
          {t('preferences.error')}
        </Banner>
      )}

      {saveErrorMsg ? (
        <div className="mb-4">
          <Banner variant="error">{saveErrorMsg}</Banner>
        </div>
      ) : null}

      {!isLoading && !isError && data ? (
        <ul className="space-y-3">
          <li>
            <ChannelToggleRow
              id="websocket"
              label={t('preferences.channel.websocket')}
              description={t('preferences.channel.websocket_desc')}
              icon={Wifi}
              checked={data.webSocketEnabled}
              onChange={(next) => {
                handleChange('webSocketEnabled', next);
              }}
            />
          </li>
          <li>
            <ChannelToggleRow
              id="email"
              label={t('preferences.channel.email')}
              description={t('preferences.channel.email_desc')}
              icon={Mail}
              checked={data.emailEnabled}
              disabled={true}
              disabledReason={t('preferences.coming_soon')}
            />
          </li>
        </ul>
      ) : null}
    </div>
  );
}
