import {
  BodyStatsForm,
  DietReminderSettingsForm,
  EnergyModel,
  GoalsForm,
  HydrationConfigForm,
  MealScheduleForm,
  WeightHistorySection,
} from '@modules/diet-planner/components/settings';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Navigate, useLocation } from 'react-router-dom';

// Legacy `?section=` deep links map onto the one scrolling page's anchors.
const LEGACY_SECTION_ANCHOR: Record<string, string> = {
  overview: 'profile',
  'body-stats': 'profile',
  'weight-history': 'profile',
  goals: 'goals',
  'meal-schedule': 'meal-times',
  hydration: 'water',
  notifications: 'reminders',
};

export default function Profile() {
  const { t } = useTranslation();
  const { search, hash } = useLocation();
  const legacy = new URLSearchParams(search).get('section');

  useEffect(() => {
    if (hash) document.getElementById(hash.slice(1))?.scrollIntoView();
  }, [hash]);

  if (legacy !== null) {
    const anchor = LEGACY_SECTION_ANCHOR[legacy] ?? 'profile';
    return <Navigate replace to={{ search: '', hash: `#${anchor}` }} />;
  }

  const sections = [
    {
      id: 'profile',
      label: t('profile.tab_profile'),
      body: (
        <>
          <div id="profile-details" className="scroll-mt-24">
            <BodyStatsForm />
          </div>
          <EnergyModel />
          <WeightHistorySection />
        </>
      ),
    },
    { id: 'goals', label: t('profile.tab_goals'), body: <GoalsForm /> },
    { id: 'meal-times', label: t('profile.tab_meal_times'), body: <MealScheduleForm /> },
    { id: 'water', label: t('profile.tab_water'), body: <HydrationConfigForm /> },
    { id: 'reminders', label: t('profile.tab_reminders'), body: <DietReminderSettingsForm /> },
  ];

  return (
    <div className="animate-fade-in mx-auto flex max-w-5xl flex-col gap-6 px-4 py-6 md:px-8">
      <div>
        <h1 className="text-26px font-bold">{t('profile.settings_title')}</h1>
        <p className="text-muted-foreground mt-1 text-sm">{t('profile.settings_subtitle')}</p>
      </div>

      <nav
        aria-label={t('profile.tabs')}
        className="bg-background sticky top-0 z-10 -mx-4 flex gap-1 overflow-x-auto border-b px-4 md:mx-0 md:px-0"
      >
        {sections.map((s) => (
          <a
            key={s.id}
            href={`#${s.id}`}
            className="text-muted-foreground hover:text-foreground focus-visible:ring-ring shrink-0 rounded-md px-4 py-2 text-sm font-medium focus-visible:ring-2 focus-visible:outline-none"
          >
            {s.label}
          </a>
        ))}
      </nav>

      {sections.map((s) => (
        <section key={s.id} id={s.id} aria-labelledby={`${s.id}-h`} className="scroll-mt-24">
          <h2 id={`${s.id}-h`} className="text-18px mb-4 font-bold">
            {s.label}
          </h2>
          <div className="flex flex-col gap-4">{s.body}</div>
        </section>
      ))}
    </div>
  );
}
