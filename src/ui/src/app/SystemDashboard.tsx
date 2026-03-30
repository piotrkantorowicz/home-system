import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@shared/components/ui';
import { getModules } from '@shared/lib/module-registry';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

export default function SystemDashboard() {
  const { t } = useTranslation();
  const modules = getModules();

  return (
    <div className="space-y-8 p-6 lg:p-8">
      <div>
        <h1 className="gradient-text text-3xl font-bold tracking-tight">
          {t('common.welcome_back', { name: '' }).replace(', !', '')}
        </h1>
        <p className="text-muted-foreground mt-1">HomeSystem Dashboard</p>
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {modules.map((mod) => {
          const Icon = mod.icon;
          return (
            <Link key={mod.name} to={mod.basePath}>
              <Card className="cursor-pointer transition-all duration-200 hover:-translate-y-1 hover:shadow-lg">
                <CardHeader>
                  <div className="flex items-center gap-3">
                    <div className="from-primary/20 to-accent/30 rounded-xl bg-gradient-to-br p-3">
                      <Icon className="text-primary h-6 w-6" />
                    </div>
                    <div>
                      <CardTitle className="text-lg capitalize">
                        {mod.name.replace(/-/g, ' ')}
                      </CardTitle>
                      <CardDescription>{mod.basePath}</CardDescription>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <p className="text-muted-foreground text-sm">
                    {mod.routes.length} pages &middot; {mod.navItems.length} nav items
                  </p>
                </CardContent>
              </Card>
            </Link>
          );
        })}
      </div>
    </div>
  );
}
