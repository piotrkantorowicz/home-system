import { computeWeightProgress } from '@modules/diet-planner/utils/computeWeightProgress';
import { normalizeWeight } from '@modules/diet-planner/utils/normalizeWeight';
import { Card, CardContent } from '@shared/components/ui';
import { ArrowDown, ArrowUp, Minus } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import type { WeightEntryDto } from '@modules/diet-planner/api/hooks/useWeightEntries';

export interface WeightProgressCardProps {
  entries: WeightEntryDto[];
  targetWeightKg: number | string | null | undefined;
}

export function WeightProgressCard({ entries, targetWeightKg }: WeightProgressCardProps) {
  const { t } = useTranslation();

  if (targetWeightKg === null || targetWeightKg === undefined || entries.length === 0) {
    return null;
  }

  const sorted = [...entries].sort((a, b) => a.date.localeCompare(b.date));
  const first = sorted[0];
  const last = sorted[sorted.length - 1];
  if (!first || !last) return null;

  const start = normalizeWeight(first.weightKg);
  const current = normalizeWeight(last.weightKg);

  const { kgRemaining, percentComplete, direction } = computeWeightProgress({
    start,
    current,
    target: normalizeWeight(targetWeightKg),
  });

  const directionColor = {
    losing: 'text-emerald-500',
    gaining: 'text-rose-500',
    maintaining: 'text-muted-foreground',
  }[direction];

  const DirectionIcon =
    direction === 'losing' ? ArrowDown : direction === 'gaining' ? ArrowUp : Minus;

  return (
    <Card>
      <CardContent className="flex flex-col gap-4 p-6 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <p className="text-muted-foreground text-sm">
            {t('weightHistory.progress.current', 'Current weight')}
          </p>
          <p className="text-3xl font-bold">{current.toFixed(1)} kg</p>
          <p className={`mt-1 flex items-center gap-1 text-sm font-medium ${directionColor}`}>
            <DirectionIcon className="h-4 w-4" />
            {t(`weightHistory.progress.direction.${direction}`, direction)}
          </p>
        </div>
        <div className="text-right">
          <p className="text-muted-foreground text-sm">
            {t('weightHistory.progress.kg_remaining', 'kg to target')}
          </p>
          <p className="text-2xl font-semibold">{kgRemaining.toFixed(1)} kg</p>
          <div className="mt-2 w-40 sm:w-56">
            <div className="bg-muted h-2 overflow-hidden rounded-full">
              <div
                className="bg-primary h-full transition-all"
                style={{ width: `${String(percentComplete)}%` }}
                data-testid="progress-fill"
              />
            </div>
            <p className="text-muted-foreground mt-1 text-xs">
              {percentComplete.toFixed(0)}% {t('weightHistory.progress.complete', 'complete')}
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
