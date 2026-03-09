import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Upload, Eye, Trash2, Calendar } from 'lucide-react';
import { useDietPlans, useDeleteDietPlan } from '@modules/diet-planner/api/hooks/useDietPlans';
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@shared/components/ui';

export default function DietPlanList() {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [planToDelete, setPlanToDelete] = useState<{ id: string; name: string } | null>(null);

  const { data, isLoading, error } = useDietPlans({ page, pageSize: 50 });
  const deleteMutation = useDeleteDietPlan();

  const handleDelete = async () => {
    if (planToDelete) {
      await deleteMutation.mutateAsync({ id: planToDelete.id });
      setDeleteDialogOpen(false);
      setPlanToDelete(null);
    }
  };

  const openDeleteDialog = (id: string, name: string) => {
    setPlanToDelete({ id, name });
    setDeleteDialogOpen(true);
  };

  return (
    <div className="p-8 lg:p-10 animate-fade-in-up">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-4xl font-bold tracking-tight mb-2">{t('diet_plans.title')}</h1>
          <p className="text-muted-foreground text-[0.95rem]">{t('diet_plans.subtitle')}</p>
        </div>
        <Link to="/diet-planner/diet-plans/import">
          <Button>
            <Upload className="mr-2 h-4 w-4" />
            {t('diet_plans.import_plan')}
          </Button>
        </Link>
      </div>

      {error && (
        <div className="mb-4 rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-destructive animate-scale-in">
          {t('common.error')}: {error.message}
        </div>
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="text-muted-foreground text-lg">{t('common.loading')}</div>
        </div>
      ) : data?.items.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-center animate-fade-in">
          <div className="rounded-2xl bg-muted/50 p-5 mb-5">
            <Calendar className="h-12 w-12 text-muted-foreground" />
          </div>
          <p className="text-xl font-semibold mb-2">{t('diet_plans.no_plans_yet')}</p>
          <p className="text-muted-foreground mb-6">{t('diet_plans.start_importing')}</p>
          <Link to="/diet-planner/diet-plans/import">
            <Button>
              <Upload className="mr-2 h-4 w-4" />
              {t('diet_plans.import_first_plan')}
            </Button>
          </Link>
        </div>
      ) : (
        <>
          <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3 stagger-children">
            {data?.items.map((plan) => (
              <Card
                key={plan.id}
                className="group hover:shadow-lg hover:-translate-y-1 transition-all duration-300"
              >
                <CardHeader>
                  <CardTitle className="text-lg">{plan.name}</CardTitle>
                  <CardDescription className="flex items-center gap-2">
                    <Calendar className="h-4 w-4" />
                    <span>
                      {new Date(plan.startDate).toLocaleDateString()} -{' '}
                      {new Date(plan.endDate).toLocaleDateString()}
                    </span>
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    <div className="grid grid-cols-2 gap-3">
                      <div className="rounded-lg bg-muted/40 p-2.5">
                        <p className="text-xs text-muted-foreground">
                          {t('diet_plans.total_days')}
                        </p>
                        <p className="font-semibold text-sm mt-0.5">{plan.totalDays}</p>
                      </div>
                      <div className="rounded-lg bg-muted/40 p-2.5">
                        <p className="text-xs text-muted-foreground">
                          {t('diet_plans.total_meals')}
                        </p>
                        <p className="font-semibold text-sm mt-0.5">{plan.totalMeals}</p>
                      </div>
                    </div>

                    <div className="text-xs text-muted-foreground">
                      {t('diet_plans.created', {
                        date: new Date(plan.createdAt).toLocaleDateString(),
                      })}
                    </div>

                    <div className="flex gap-2 pt-3 border-t">
                      <Link to={`/diet-planner/diet-plans/${plan.id}`} className="flex-1">
                        <Button variant="outline" size="sm" className="w-full">
                          <Eye className="mr-2 h-4 w-4" />
                          {t('diet_plans.view_calendar')}
                        </Button>
                      </Link>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => openDeleteDialog(plan.id, plan.name)}
                        className="hover:text-destructive hover:border-destructive/30"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          {data && data.totalPages > 1 && (
            <div className="mt-6 flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {t('common.showing_range', {
                  start: (page - 1) * 50 + 1,
                  end: Math.min(page * 50, data.totalCount),
                  total: data.totalCount,
                })}
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page === 1}
                >
                  {t('common.previous')}
                </Button>
                <Button
                  variant="outline"
                  onClick={() => setPage((p) => p + 1)}
                  disabled={page >= data.totalPages}
                >
                  {t('common.next')}
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('diet_plans.delete_dialog.title')}</DialogTitle>
            <DialogDescription>
              {t('diet_plans.delete_dialog.description', { name: planToDelete?.name })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteDialogOpen(false)}>
              {t('common.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending
                ? t('diet_plans.delete_dialog.deleting')
                : t('diet_plans.delete_dialog.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
