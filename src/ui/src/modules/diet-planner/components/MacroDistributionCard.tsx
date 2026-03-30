import { Card, CardContent, CardHeader, CardTitle } from '@shared/components/ui';

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
  {
    key: 'protein' as const,
    labelKey: 'products.table.protein',
    gradient: 'from-blue-500 to-indigo-500',
  },
  {
    key: 'carbs' as const,
    labelKey: 'product_detail.carbohydrates',
    gradient: 'from-emerald-500 to-teal-500',
  },
  { key: 'fat' as const, labelKey: 'product_detail.fat', gradient: 'from-amber-500 to-orange-500' },
  {
    key: 'fiber' as const,
    labelKey: 'products.table.fiber',
    gradient: 'from-violet-500 to-purple-500',
  },
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
                <div className="mb-2 flex justify-between text-[0.9rem]">
                  <span className="font-medium">{t(macro.labelKey)}</span>
                  <span className="text-muted-foreground">
                    {val.toFixed(1)}g<span className="ml-1.5 text-xs">({percent.toFixed(0)}%)</span>
                  </span>
                </div>
                <div className="bg-muted h-2.5 overflow-hidden rounded-full">
                  <div
                    className={`h-full bg-gradient-to-r ${macro.gradient} animate-bar-fill rounded-full`}
                    style={{ width: `${String(percent)}%` }}
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
