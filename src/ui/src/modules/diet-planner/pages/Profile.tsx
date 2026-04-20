import {
  BodyStatsForm,
  GoalsForm,
  MealScheduleForm,
  HydrationConfigForm,
  NotificationPrefsForm,
} from '@modules/diet-planner/components/settings';
import { cn } from '@shared/lib/utils';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';

type SectionId = 'body-stats' | 'goals' | 'meal-schedule' | 'hydration' | 'notifications';

function isValidSection(s: string | null): s is SectionId {
  return ['body-stats', 'goals', 'meal-schedule', 'hydration', 'notifications'].includes(s ?? '');
}

const SECTION_COMPONENTS: Record<SectionId, React.ComponentType<{ onSuccess?: () => void }>> = {
  'body-stats': BodyStatsForm,
  goals: GoalsForm,
  'meal-schedule': MealScheduleForm,
  hydration: HydrationConfigForm,
  notifications: NotificationPrefsForm,
};

export default function Profile() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const rawSection = searchParams.get('section');
  const activeSection: SectionId = isValidSection(rawSection) ? rawSection : 'body-stats';

  function navigate(section: SectionId) {
    setSearchParams({ section });
  }

  const sidebarGroups = [
    {
      label: t('profile.sidebar.personal'),
      items: [{ id: 'body-stats' as SectionId, label: t('profile.sidebar.body_stats') }],
    },
    {
      label: t('profile.sidebar.diet_planner'),
      items: [
        { id: 'goals' as SectionId, label: t('profile.sidebar.goals') },
        { id: 'meal-schedule' as SectionId, label: t('profile.sidebar.meal_schedule') },
        { id: 'hydration' as SectionId, label: t('profile.sidebar.hydration') },
      ],
    },
    {
      label: t('profile.sidebar.notifications'),
      items: [{ id: 'notifications' as SectionId, label: t('profile.sidebar.alerts') }],
    },
  ];

  const allItems = sidebarGroups.flatMap((g) => g.items);

  const ActiveForm = SECTION_COMPONENTS[activeSection];

  return (
    <div className="animate-fade-in-up p-8 lg:p-10">
      {/* Hero */}
      <div className="mb-8">
        <h1 className="mb-2 text-4xl font-bold tracking-tight">{t('profile.page_title')}</h1>
        <p className="text-muted-foreground text-lg">{t('profile.page_subtitle')}</p>
      </div>

      <div className="flex gap-8">
        {/* Desktop sidebar */}
        <nav className="hidden w-56 shrink-0 md:block">
          {sidebarGroups.map((group) => (
            <div key={group.label} className="mb-6">
              <p className="text-muted-foreground mb-2 px-3 text-xs font-semibold tracking-wider uppercase">
                {group.label}
              </p>
              <ul>
                {group.items.map((item) => (
                  <li key={item.id}>
                    <button
                      onClick={() => {
                        navigate(item.id);
                      }}
                      className={cn(
                        'w-full rounded-lg px-3 py-2 text-left text-sm transition-colors',
                        activeSection === item.id
                          ? 'bg-primary/10 text-primary font-medium'
                          : 'text-muted-foreground hover:bg-accent/50 hover:text-foreground',
                      )}
                    >
                      {item.label}
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </nav>

        {/* Content */}
        <div className="min-w-0 flex-1">
          {/* Mobile tabs */}
          <div className="mb-6 flex gap-1 overflow-x-auto border-b md:hidden">
            {allItems.map((item) => (
              <button
                key={item.id}
                onClick={() => {
                  navigate(item.id);
                }}
                className={cn(
                  'shrink-0 border-b-2 px-4 py-2 text-sm font-medium transition-colors',
                  activeSection === item.id
                    ? 'border-primary text-primary'
                    : 'text-muted-foreground hover:text-foreground border-transparent',
                )}
              >
                {item.label}
              </button>
            ))}
          </div>

          {/* Active section form */}
          <ActiveForm />
        </div>
      </div>
    </div>
  );
}
