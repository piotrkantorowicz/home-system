import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@shared/components/ui';
import { cn } from '@shared/lib/utils';
import { Clock, MoreVertical, Pencil, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

type Macro = 'protein' | 'carbs' | 'fat';

export interface RecipeCardData {
  id: string;
  name: string;
  servings: number | string;
  prepTimeMinutes?: number | string | null;
  isOwner: boolean;
  nutritionPerServing?: {
    calories: number | string;
    protein: number | string;
    carbs: number | string;
    fat: number | string;
  } | null;
}

const n = (v: number | string | null | undefined): number =>
  typeof v === 'number' ? v : Number(v ?? 0);

function dominant(r: RecipeCardData): Macro {
  const p = r.nutritionPerServing;
  const v = { protein: n(p?.protein), carbs: n(p?.carbs), fat: n(p?.fat) };
  return (['protein', 'carbs', 'fat'] as const).reduce((a, b) => (v[b] > v[a] ? b : a));
}

export function RecipeCard({ recipe, onDelete }: { recipe: RecipeCardData; onDelete: () => void }) {
  const { t } = useTranslation();
  const macro = dominant(recipe);
  const per = recipe.nutritionPerServing;

  const strip: { key: 'kcal' | Macro; value: string; label: string }[] = [
    { key: 'kcal', value: n(per?.calories).toFixed(0), label: 'kcal' },
    { key: 'protein', value: n(per?.protein).toFixed(0), label: 'P' },
    { key: 'carbs', value: n(per?.carbs).toFixed(0), label: 'C' },
    { key: 'fat', value: n(per?.fat).toFixed(0), label: 'F' },
  ];

  return (
    <div className="border-border bg-card flex flex-col overflow-hidden rounded-[22px] border shadow-sm">
      <div
        className="relative h-[132px]"
        style={{
          background: `linear-gradient(140deg, color-mix(in oklab, hsl(var(--color-${macro})) 45%, transparent), color-mix(in oklab, hsl(var(--color-${macro})) 12%, transparent))`,
        }}
      >
        {recipe.prepTimeMinutes ? (
          <span className="bg-card absolute top-2.5 left-2.5 inline-flex items-center gap-1 rounded-[8px] px-2 py-1 text-[11px] font-semibold shadow-sm">
            <Clock className="size-3" />
            {n(recipe.prepTimeMinutes)} {t('recipes.prep_time')}
          </span>
        ) : null}
        <div className="absolute top-2.5 right-2.5">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                aria-label={t('common.actions')}
                className="bg-card grid size-7 place-items-center rounded-full shadow-sm"
              >
                <MoreVertical className="size-4" />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link to={`/diet-planner/recipes/${recipe.id}`}>{t('common.view')}</Link>
              </DropdownMenuItem>
              {recipe.isOwner ? (
                <>
                  <DropdownMenuItem asChild>
                    <Link to={`/diet-planner/recipes/${recipe.id}/edit`}>
                      <Pencil className="size-4" />
                      {t('common.edit')}
                    </Link>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onSelect={onDelete}
                    className="text-destructive focus:text-destructive"
                  >
                    <Trash2 className="size-4" />
                    {t('common.delete')}
                  </DropdownMenuItem>
                </>
              ) : null}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      <div className="flex flex-col gap-3 p-4">
        <div>
          <Link
            to={`/diet-planner/recipes/${recipe.id}`}
            className="text-[14.5px] font-bold hover:underline"
          >
            {recipe.name}
          </Link>
          <div className="text-muted-foreground text-[11.5px]">
            {t('recipes.servings', { count: n(recipe.servings) })}
          </div>
        </div>

        <div className="grid grid-cols-4 gap-1.5">
          {strip.map((cell) => (
            <div
              key={cell.key}
              className={cn(
                'rounded-[9px] p-1.5 text-center',
                cell.key === 'kcal' && 'bg-secondary border-border border',
              )}
              style={
                cell.key === 'kcal'
                  ? undefined
                  : {
                      background: `color-mix(in oklab, hsl(var(--color-${cell.key})) 12%, transparent)`,
                    }
              }
            >
              <div className="tnum text-[13px] font-bold">{cell.value}</div>
              <div className="text-muted-foreground text-[9.5px]">{cell.label}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
