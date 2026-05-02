import { Mail, Monitor, Wifi } from 'lucide-react';
import { useCallback, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { useChannelPreferences } from '../api/hooks/useChannelPreferences';
import { useUpdateChannelPreferences } from '../api/hooks/useUpdateChannelPreferences';
import { ChannelToggleRow } from '../components/ChannelToggleRow';

import type { ChannelPreferencesDto } from '../api/hooks/useChannelPreferences';

const DEBOUNCE_MS = 250;

export default function ChannelPreferences() {
  const { t } = useTranslation('notifications');
  const { data, isLoading, isError, refetch } = useChannelPreferences();
  const update = useUpdateChannelPreferences();
  const saveErrorMsg = update.isError ? t('preferences.save_failed') : null;

  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleChange = useCallback(
    (field: keyof ChannelPreferencesDto, next: boolean) => {
      if (!data) return;

      const updated: ChannelPreferencesDto = { ...data, [field]: next };

      if (debounceRef.current !== null) clearTimeout(debounceRef.current);
      debounceRef.current = setTimeout(() => {
        update.mutate(updated);
      }, DEBOUNCE_MS);
    },
    [data, update],
  );

  useEffect(() => {
    return () => {
      if (debounceRef.current !== null) clearTimeout(debounceRef.current);
    };
  }, []);

  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-6">
      <header className="mb-6 flex items-baseline justify-between">
        <div>
          <h1 className="text-text text-2xl font-semibold">{t('preferences.title')}</h1>
          <p className="text-text-muted text-sm">{t('preferences.subtitle')}</p>
        </div>
        <Link
          to="/notifications"
          className="text-primary focus-visible:ring-primary text-sm hover:underline focus-visible:ring-2 focus-visible:outline-none"
        >
          ← {t('preferences.back_to_inbox')}
        </Link>
      </header>

      {isLoading && (
        <ul aria-busy="true" aria-label={t('preferences.loading')} className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <li key={i} className="bg-surface-alt h-20 animate-pulse rounded-md" />
          ))}
        </ul>
      )}

      {isError && (
        <div
          role="alert"
          className="border-error/40 bg-error/10 text-text flex items-center justify-between rounded-md border p-4 text-sm"
        >
          <p>{t('preferences.error')}</p>
          <button
            type="button"
            onClick={() => void refetch()}
            className="border-error/40 focus-visible:ring-primary hover:bg-error/20 rounded-md border px-3 py-1 text-sm focus-visible:ring-2 focus-visible:outline-none"
          >
            {t('preferences.retry')}
          </button>
        </div>
      )}

      {saveErrorMsg ? (
        <div role="alert" aria-live="polite" className="mb-4">
          <p className="text-error text-sm">{saveErrorMsg}</p>
        </div>
      ) : null}

      {!isLoading && !isError && data ? (
        <ul className="space-y-3">
          <li>
            <ChannelToggleRow
              id="console"
              label={t('preferences.channel.console')}
              description={t('preferences.channel.console_desc')}
              icon={Monitor}
              checked={data.consoleEnabled}
              onChange={(next) => {
                handleChange('consoleEnabled', next);
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
          <li>
            <ChannelToggleRow
              id="websocket"
              label={t('preferences.channel.websocket')}
              description={t('preferences.channel.websocket_desc')}
              icon={Wifi}
              checked={data.webSocketEnabled}
              disabled={true}
              disabledReason={t('preferences.coming_soon')}
            />
          </li>
        </ul>
      ) : null}

      <footer className="mt-8">
        <Link
          to="/diet-planner/reminders"
          className="text-primary focus-visible:ring-primary text-sm hover:underline focus-visible:ring-2 focus-visible:outline-none"
        >
          {t('preferences.diet_settings_link')}
        </Link>
      </footer>
    </main>
  );
}
