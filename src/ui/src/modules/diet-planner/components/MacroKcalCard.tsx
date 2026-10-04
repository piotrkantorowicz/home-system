import { Card } from '@shared/components/ui';
import { useTranslation } from 'react-i18next';

import type { KcalSplit } from '../utils/nutritionRange';

const MACROS = ['protein', 'carbs', 'fat'] as const;

function Bar({ label, split }: { label: string; split: KcalSplit | null }) {
  return (
    <div className="flex flex-col gap-1.5">
      <div className="text-muted-foreground text-label font-semibold">{label}</div>
      {split ? (
        <div className="flex h-8 overflow-hidden rounded-md">
          {MACROS.map((m) => (
            <div
              key={m}
              className="text-primary-foreground text-label tnum flex items-center justify-center font-semibold"
              style={{ width: `${String(split[m])}%`, background: `var(--color-${m})` }}
            >
              {split[m] >= 8 ? `${String(split[m])}%` : null}
            </div>
          ))}
        </div>
      ) : (
        <div className="border-border-strong h-8 rounded-md border border-dashed" />
      )}
    </div>
  );
}

interface MacroKcalCardProps {
  actual: KcalSplit | null;
  goal: KcalSplit | null;
}

/** Where calories come from: protein/carbs/fat as a share of kcal, eaten vs goals. */
export function MacroKcalCard({ actual, goal }: MacroKcalCardProps) {
  const { t } = useTranslation();

  // Plain-language line: the macro whose share differs most from the goal.
  let sentence = t('nutrition_page.macro_sentence_none');
  if (actual && goal) {
    const biggest = MACROS.reduce((a, b) =>
      Math.abs(actual[b] - goal[b]) > Math.abs(actual[a] - goal[a]) ? b : a,
    );
    sentence = t('nutrition_page.macro_sentence', {
      macro: t(`nutrition_page.macro_${biggest}`),
      actual: actual[biggest],
      goal: goal[biggest],
    });
  } else if (actual) {
    sentence = t('nutrition_page.macro_sentence_no_goal');
  }

  return (
    <Card className="flex flex-col gap-4 p-6">
      <div className="text-15px font-bold">{t('nutrition_page.macro_split')}</div>
      <Bar label={t('nutrition_page.actual')} split={actual} />
      <Bar label={t('nutrition_page.target')} split={goal} />
      <ul className="text-muted-foreground text-label flex flex-wrap gap-4">
        {MACROS.map((m) => (
          <li key={m} className="inline-flex items-center gap-1.5">
            <span className="size-2 rounded-full" style={{ background: `var(--color-${m})` }} />
            {t(`nutrition_page.macro_${m}`)}
          </li>
        ))}
      </ul>
      <p className="text-text-2 text-sm">{sentence}</p>
    </Card>
  );
}
