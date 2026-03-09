import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@shared/components/ui/Dialog';
import { Button } from '@shared/components/ui/Button';
import { Input } from '@shared/components/ui/Input';
import { Label } from '@shared/components/ui/Label';

interface DietPlanFormData {
  name: string;
  startDate: string;
  endDate: string;
}

interface DietPlanFormProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: DietPlanFormData) => void;
  isSubmitting?: boolean;
}

export function DietPlanForm({ open, onClose, onSubmit, isSubmitting }: DietPlanFormProps) {
  const { t } = useTranslation();
  const [form, setForm] = useState<DietPlanFormData>({
    name: '',
    startDate: '',
    endDate: '',
  });

  const isValid =
    !!form.name.trim() && !!form.startDate && !!form.endDate && form.endDate >= form.startDate;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!isValid) return;
    onSubmit(form);
  };

  const handleClose = () => {
    setForm({ name: '', startDate: '', endDate: '' });
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(v) => !v && handleClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{t('diet_plan_form.title')}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="plan-name">{t('diet_plan_form.name_label')}</Label>
            <Input
              id="plan-name"
              placeholder={t('diet_plan_form.name_placeholder')}
              value={form.name}
              onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="plan-start-date">{t('diet_plan_form.start_date_label')}</Label>
              <Input
                id="plan-start-date"
                type="date"
                value={form.startDate}
                onChange={(e) => setForm((f) => ({ ...f, startDate: e.target.value }))}
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="plan-end-date">{t('diet_plan_form.end_date_label')}</Label>
              <Input
                id="plan-end-date"
                type="date"
                value={form.endDate}
                min={form.startDate}
                onChange={(e) => setForm((f) => ({ ...f, endDate: e.target.value }))}
                required
              />
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose} disabled={isSubmitting}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" disabled={!isValid || isSubmitting}>
              {isSubmitting ? t('common.saving') : t('diet_plan_form.create_btn')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
