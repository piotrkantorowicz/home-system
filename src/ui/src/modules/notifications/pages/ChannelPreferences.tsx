import { Banner, Button } from '@shared/components/ui';
import { Mail, Wifi } from 'lucide-react';
import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { useChannelPreferences } from '../api/hooks/useChannelPreferences';
import { useUpdateChannelPreferences } from '../api/hooks/useUpdateChannelPreferences';
import { ChannelToggleRow } from '../components/ChannelToggleRow';

import type { ChannelPreferencesDto } from '../api/hooks/useChannelPreferences';

export default function ChannelPreferences() {
  const { t } = useTranslation('notifications');
  const { data, isLoading, isError, refetch } = useChannelPreferences();
  const update = useUpdateChannelPreferences();
  const saveErrorMsg = update.isError ? t('preferences.save_failed') : null;

  const handleChange = useCallback(
    (field: keyof ChannelPreferencesDto, next: boolean) => {
      if (!data) return;
      const updated: ChannelPreferencesDto = { ...data, [field]: next };
      update.mutate(updated);
    },
    [data, update],
  );

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-6 md:px-8">
      <header className="mb-6 flex items-end justify-between gap-4">
        <div>
          <h1 className="text-[26px] font-bold tracking-tight">{t('preferences.title')}</h1>
          <p className="text-muted-foreground mt-0.5 text-sm">{t('preferences.subtitle')}</p>
        </div>
        <Button asChild variant="secondary" size="sm">
          <Link to="/notifications">{t('preferences.back_to_inbox')}</Link>
        </Button>
      </header>

      {isLoading && (
        <ul aria-busy="true" aria-label={t('preferences.loading')} className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <li key={i} className="bg-muted h-20 animate-pulse rounded-[13px]" />
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
              id="email"
              label={t('preferences.channel.email')}
              description={t('preferences.channel.email_desc')}
              icon={Mail}
              checked={data.emailEnabled}
              disabled={true}
              disabledReason={t('preferences.coming_soon')}
            />
          </li>
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
        </ul>
      ) : null}

      <footer className="mt-8">
        <Link
          to="/diet-planner/profile?section=notifications"
          className="text-primary focus-visible:ring-primary text-sm hover:underline focus-visible:ring-2 focus-visible:outline-none"
        >
          {t('preferences.diet_settings_link')}
        </Link>
      </footer>
    </main>
  );
}
