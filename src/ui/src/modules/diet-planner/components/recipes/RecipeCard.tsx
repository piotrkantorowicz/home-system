import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@shared/components/ui';
import { useFormat } from '@shared/hooks/useFormat';
import { Clock, MoreVertical, Pencil, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { VisibilityBadge } from '../VisibilityBadge';

type NumLike = number | string | null | undefined;

export interface RecipeCardData {
  id: string;
  name: string;
  servings: number | string;
  prepTimeMinutes?: NumLike;
  visibility: string;
  canEdit: boolean;
  nutritionPerServing?: {
    calories: NumLike;
    protein: NumLike;
    carbs: NumLike;
    fat: NumLike;
  } | null;
}

const num = (v: NumLike): number | null => {
  if (v === null || v === undefined || v === '') return null;
  const x = Number(v);
  return Number.isFinite(x) ? x : null;
};

/** Share of energy from each macro (protein/carbs 4 kcal/g, fat 9 kcal/g); null when it cannot be computed. */
function macroEnergyShares(
  per: RecipeCardData['nutritionPerServing'],
): { protein: number; carbs: number; fat: number } | null {
  const protein = num(per?.protein);
  const carbs = num(per?.carbs);
  const fat = num(per?.fat);
  if (protein === null || carbs === null || fat === null) return null;
  const p = protein * 4;
  const c = carbs * 4;
  const f = fat * 9;
  const total = p + c + f;
  if (total <= 0) return null;
  return { protein: (p / total) * 100, carbs: (c / total) * 100, fat: (f / total) * 100 };
}

export interface RecipeCardProps {
  recipe: RecipeCardData;
  /** Fired on hover / focus of the detail link — warm the detail query before navigation. */
  onPrefetch?: () => void;
  onDelete: () => void;
}

export function RecipeCard({ recipe, onPrefetch, onDelete }: RecipeCardProps) {
  const { t } = useTranslation();
  const fmt = useFormat();
  const per = recipe.nutritionPerServing;
  const shares = macroEnergyShares(per);
  const prep = num(recipe.prepTimeMinutes);
  const servings = num(recipe.servings) ?? 1;

  const meta = [
    prep !== null && prep > 0 ? t('recipes.prep_minutes', { count: prep }) : null,
    t('recipes.servings', { count: servings }),
  ].filter((x): x is string => x !== null);

  return (
    <div
      role="listitem"
      aria-label={recipe.name}
      className="border-border bg-card hover:border-primary focus-within:ring-primary rounded-22px relative flex flex-col gap-2.5 border p-4 shadow-sm focus-within:ring-2"
    >
      <div className="flex items-start justify-between gap-2">
        <Link
          to={`/diet-planner/recipes/${recipe.id}`}
          className="after:rounded-22px line-clamp-2 text-[14.5px] font-bold after:absolute after:inset-0 focus:outline-none"
          onMouseEnter={onPrefetch}
          onFocus={onPrefetch}
        >
          {recipe.name}
        </Link>
        <div className="relative z-10 -mt-2 -mr-2 shrink-0">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                aria-label={t('common.actions')}
                className="text-muted-foreground hover:text-foreground grid size-11 place-items-center rounded-full"
              >
                <MoreVertical className="size-4" />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link to={`/diet-planner/recipes/${recipe.id}`}>{t('common.view')}</Link>
              </DropdownMenuItem>
              {recipe.canEdit ? (
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

      <div className="text-muted-foreground text-11-5px flex flex-wrap items-center gap-2">
        {prep !== null && prep > 0 ? <Clock className="size-3" aria-hidden /> : null}
        <span>{meta.join(' · ')}</span>
        {recipe.visibility === 'Household' ? null : (
          <VisibilityBadge visibility={recipe.visibility} />
        )}
      </div>

      <div className="tnum text-[22px] leading-none font-bold">
        {fmt.energy(num(per?.calories))}
      </div>

      {shares ? (
        <div
          role="img"
          aria-label={t('recipes.macro_bar_aria', {
            protein: Math.round(shares.protein),
            carbs: Math.round(shares.carbs),
            fat: Math.round(shares.fat),
          })}
          className="bg-muted flex h-2 overflow-hidden rounded-full"
        >
          <span className="bg-protein" style={{ width: `${String(shares.protein)}%` }} />
          <span className="bg-carbs" style={{ width: `${String(shares.carbs)}%` }} />
          <span className="bg-fat" style={{ width: `${String(shares.fat)}%` }} />
        </div>
      ) : null}

      <div className="text-text-2 text-12px tnum">
        {t('recipes.macro_line', {
          protein: fmt.grams(num(per?.protein)),
          carbs: fmt.grams(num(per?.carbs)),
          fat: fmt.grams(num(per?.fat)),
        })}
      </div>
    </div>
  );
}
