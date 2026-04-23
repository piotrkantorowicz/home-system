import {
  useDeleteWeightEntry,
  type WeightEntryDto,
} from '@modules/diet-planner/api/hooks/useWeightEntries';
import { normalizeWeight } from '@modules/diet-planner/utils/normalizeWeight';
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@shared/components/ui';
import { useToast } from '@shared/context/ToastContext';
import { Loader2, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

export interface WeightEntryListProps {
  entries: WeightEntryDto[];
  limit?: number;
}

export function WeightEntryList({ entries, limit = 10 }: WeightEntryListProps) {
  const { t, i18n } = useTranslation();
  const toast = useToast();
  const deleteMutation = useDeleteWeightEntry();
  const [pendingDelete, setPendingDelete] = useState<WeightEntryDto | null>(null);

  const recent = [...entries].sort((a, b) => b.date.localeCompare(a.date)).slice(0, limit);

  const fmt = new Intl.DateTimeFormat(i18n.language, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

  const confirmDelete = async () => {
    if (!pendingDelete) return;
    try {
      await deleteMutation.mutateAsync(pendingDelete.id);
      toast.success(t('weightHistory.delete_success', 'Entry removed'));
      setPendingDelete(null);
    } catch {
      toast.error(t('weightHistory.delete_error', 'Failed to delete entry'));
    }
  };

  if (recent.length === 0) {
    return null;
  }

  return (
    <>
      <ul className="divide-border divide-y rounded-lg border">
        {recent.map((entry) => (
          <li
            key={entry.id}
            className="flex items-center justify-between px-4 py-3"
            data-testid={`weight-entry-${entry.id}`}
          >
            <div>
              <p className="font-medium">{fmt.format(new Date(entry.date))}</p>
              <p className="text-muted-foreground text-sm">
                {normalizeWeight(entry.weightKg).toFixed(1)} kg
              </p>
            </div>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => {
                setPendingDelete(entry);
              }}
              aria-label={t('weightHistory.delete_aria', 'Delete entry')}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </li>
        ))}
      </ul>

      <Dialog
        open={pendingDelete !== null}
        onOpenChange={(open) => {
          if (!open) setPendingDelete(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {t('weightHistory.delete_confirm_title', 'Delete weight entry?')}
            </DialogTitle>
            <DialogDescription>
              {t(
                'weightHistory.delete_confirm_body',
                'This will permanently remove this entry and recalculate your current weight from the remaining entries.',
              )}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => {
                setPendingDelete(null);
              }}
            >
              {t('common.cancel', 'Cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                void confirmDelete();
              }}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  {t('common.deleting', 'Deleting…')}
                </>
              ) : (
                t('common.delete', 'Delete')
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
