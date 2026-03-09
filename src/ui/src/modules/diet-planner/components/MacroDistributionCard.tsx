import type { TFunction } from 'i18next';
import { Card, CardContent, CardHeader, CardTitle } from '@shared/components/ui';

interface MacroDistributionCardProps {
  protein: number;
  carbs: number;
  fat: number;
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
];

export function MacroDistributionCard({
  protein,
  carbs,
  fat,
  t,
  title,
  className,
}: MacroDistributionCardProps) {
  const values = { protein, carbs, fat };
  const total = protein + carbs + fat;
  const pct = (val: number) => (total > 0 ? (val / total) * 100 : 0);

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>{title || t('product_detail.macro_distribution')}</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {macros.map((macro) => {
            const val = values[macro.key];
            const percent = pct(val);
            return (
              <div key={macro.key}>
                <div className="flex justify-between text-[0.9rem] mb-2">
                  <span className="font-medium">{t(macro.labelKey)}</span>
                  <span className="text-muted-foreground">
                    {val.toFixed(1)}g<span className="ml-1.5 text-xs">({percent.toFixed(0)}%)</span>
                  </span>
                </div>
                <div className="h-2.5 bg-muted rounded-full overflow-hidden">
                  <div
                    className={`h-full bg-gradient-to-r ${macro.gradient} rounded-full animate-bar-fill`}
                    style={{ width: `${percent}%` }}
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
