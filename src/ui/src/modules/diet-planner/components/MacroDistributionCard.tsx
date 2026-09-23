import { Card, CardContent, CardHeader, CardTitle } from '@shared/components/ui';
import { formatNumber } from '@shared/lib/utils';

import type { TFunction } from 'i18next';

interface MacroDistributionCardProps {
  protein: number;
  carbs: number;
  fat: number;
  fiber?: number;
  t: TFunction;
  title?: string;
  className?: string;
}

const macros = [
  { key: 'protein' as const, labelKey: 'products.table.protein' },
  { key: 'carbs' as const, labelKey: 'product_detail.carbohydrates' },
  { key: 'fat' as const, labelKey: 'product_detail.fat' },
  { key: 'fiber' as const, labelKey: 'products.table.fiber' },
];

export function MacroDistributionCard({
  protein,
  carbs,
  fat,
  fiber = 0,
  t,
  title,
  className,
}: MacroDistributionCardProps) {
  const values = { protein, carbs, fat, fiber };
  const total = protein + carbs + fat;
  const pct = (val: number) => (total > 0 ? (val / total) * 100 : 0);

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>{title ?? t('product_detail.macro_distribution')}</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {macros.map((macro) => {
            const val = values[macro.key];
            const percent = pct(val);
            return (
              <div key={macro.key}>
                <div className="text-0-9rem mb-2 flex justify-between">
                  <span className="font-medium">{t(macro.labelKey)}</span>
                  <span className="text-muted-foreground tnum">
                    {val.toFixed(1)} g
                    <span className="ml-1.5 text-xs">({formatNumber(percent)}%)</span>
                  </span>
                </div>
                <div className="bg-muted h-2.5 overflow-hidden rounded-full">
                  <div
                    className="h-full rounded-full"
                    style={{
                      width: `${String(percent)}%`,
                      background: `var(--color-${macro.key})`,
                    }}
                  />
                </div>
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
