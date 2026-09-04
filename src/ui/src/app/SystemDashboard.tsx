import { getModules } from '@shared/lib/module-registry';
import { ArrowRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';
import { Link } from 'react-router-dom';

export default function SystemDashboard() {
  const { t } = useTranslation();
  const auth = useAuth();
  const modules = getModules();

  const displayName =
    auth.user?.profile.name ??
    auth.user?.profile.preferred_username ??
    auth.user?.profile.email ??
    'User';

  return (
    <div className="animate-fade-in flex flex-col gap-6 px-4 py-6 md:px-8">
      <div>
        <h1 className="text-[26px] font-bold">{t('common.welcome_back', { name: displayName })}</h1>
        <p className="text-muted-foreground mt-1 text-sm">HomeSystem</p>
      </div>

      <div className="grid [grid-template-columns:repeat(auto-fit,minmax(280px,1fr))] gap-[18px]">
        {modules.map((mod) => {
          const Icon = mod.icon;
          return (
            <Link
              key={mod.name}
              to={mod.basePath}
              className="border-border bg-card group flex flex-col gap-3 rounded-[22px] border p-[22px] shadow-sm transition-colors"
            >
              <div className="flex items-center justify-between">
                <span className="bg-accent text-accent-foreground grid size-11 place-items-center rounded-2xl">
                  <Icon className="size-5" />
                </span>
                <ArrowRight className="text-muted-foreground size-4 -translate-x-1 opacity-0 transition-all group-hover:translate-x-0 group-hover:opacity-100" />
              </div>
              <div>
                <div className="text-[15px] font-bold capitalize">
                  {mod.name.replace(/-/g, ' ')}
                </div>
                <p className="text-muted-foreground text-[12.5px]">
                  {mod.description ?? mod.basePath}
                </p>
              </div>
            </Link>
          );
        })}
      </div>
    </div>
  );
}
