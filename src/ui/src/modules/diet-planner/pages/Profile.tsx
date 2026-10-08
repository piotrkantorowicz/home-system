import {
  BodyStatsForm,
  DietReminderSettingsForm,
  EnergyModel,
  GoalsForm,
  HydrationConfigForm,
  MealScheduleForm,
  WeightHistorySection,
} from '@modules/diet-planner/components/settings';
import { PageContainer } from '@shared/components/ui';
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Navigate, useLocation } from 'react-router-dom';

// Legacy `?section=` deep links map onto the tab hashes.
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

  // In-tab anchors (e.g. EnergyModel's #profile-details) still scroll into view.
  useEffect(() => {
    if (hash) document.getElementById(hash.slice(1))?.scrollIntoView();
  }, [hash]);

  if (legacy !== null) {
    const anchor = LEGACY_SECTION_ANCHOR[legacy] ?? 'profile';
    return <Navigate replace to={{ search: '', hash: `#${anchor}` }} />;
  }

  const profileTab = {
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
  };
  const sections = [
    profileTab,
    { id: 'goals', label: t('profile.tab_goals'), body: <GoalsForm /> },
    { id: 'meal-times', label: t('profile.tab_meal_times'), body: <MealScheduleForm /> },
    { id: 'water', label: t('profile.tab_water'), body: <HydrationConfigForm /> },
    { id: 'reminders', label: t('profile.tab_reminders'), body: <DietReminderSettingsForm /> },
  ];
  const active = sections.find((s) => `#${s.id}` === hash) ?? profileTab;

  return (
    <PageContainer className="animate-fade-in flex flex-col gap-6">
      <div>
        <h1 className="text-26px font-bold">{t('profile.settings_title')}</h1>
        <p className="text-muted-foreground mt-1 text-sm">{t('profile.settings_subtitle')}</p>
      </div>

      <nav
        aria-label={t('profile.tabs')}
        className="-mx-4 flex gap-1 overflow-x-auto border-b px-4 md:mx-0 md:px-0"
      >
        {sections.map((s) => (
          <a
            key={s.id}
            href={`#${s.id}`}
            aria-current={s === active ? 'page' : undefined}
            className="text-muted-foreground hover:text-foreground focus-visible:ring-ring aria-[current=page]:border-primary aria-[current=page]:text-foreground shrink-0 rounded-t-md border-b-2 border-transparent px-4 py-2 text-sm font-medium focus-visible:ring-2 focus-visible:outline-none"
          >
            {s.label}
          </a>
        ))}
      </nav>

      <section aria-labelledby={`${active.id}-h`}>
        <h2 id={`${active.id}-h`} className="text-18px mb-4 font-bold">
          {active.label}
        </h2>
        <div className="flex flex-col gap-4">{active.body}</div>
      </section>
    </PageContainer>
  );
}
