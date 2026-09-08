import { cn } from '@shared/lib/utils';
import { Minus, Plus } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export const DEFAULT_TARGET_ML = 2500;
export const DEFAULT_GLASS_ML = 250;
export const MAX_GLASSES = 12;

export interface GlassRowProps {
  totalMl: number;
  targetMl: number;
  glassMl: number;
  /** `sm` = dashboard card (44px), `lg` = the Hydration page (60px). */
  size?: 'sm' | 'lg';
  /** Tap an empty glass — adds one `glassMl`. */
  onAdd: () => void;
  /** Tap a filled glass — removes the newest logged entry. */
  onRemoveNewest?: () => void;
  informational?: boolean;
  addDisabled?: boolean;
  removeDisabled?: boolean;
  className?: string;
}

/**
 * One row of glasses shared by the dashboard WaterCard and the Hydration
 * page (BUILD_REVIEW.md #fix-water item 7 — "one GlassRow, one geometry").
 * Filled glasses remove on tap; the first empty glass adds on tap. Partial
 * glasses print the real millilitres and aren't interactive — there's no
 * single logged entry they map back to.
 */
export function GlassRow({
  totalMl,
  informational = false,
  targetMl,
  glassMl,
  size = 'lg',
  onAdd,
  onRemoveNewest,
  addDisabled = false,
  removeDisabled = false,
  className,
}: GlassRowProps) {
  const { t } = useTranslation();

  if (informational) {
    const percent = targetMl > 0 ? Math.min(100, Math.max(0, (totalMl / targetMl) * 100)) : 0;
    return (
      <div
        role="progressbar"
        aria-label={t('dashboard.water_title')}
        aria-valuemin={0}
        aria-valuemax={targetMl}
        aria-valuenow={Math.min(totalMl, targetMl)}
        className="bg-muted h-2 overflow-hidden rounded-full"
      >
        <div
          className="h-full rounded-full bg-[var(--color-water)]"
          style={{ width: `${String(percent)}%` }}
        />
      </div>
    );
  }

  const targetGlasses = Math.min(
    MAX_GLASSES,
    Math.max(1, Math.round(targetMl / Math.max(glassMl, 1))),
  );
  const filled = Math.floor(totalMl / glassMl);
  const partialMl = Math.round(totalMl - filled * glassMl);
  const hasPartial = partialMl > 0;

  const heightClass = size === 'sm' ? 'h-11' : 'h-[60px]';
  const radiusClass = size === 'sm' ? 'rounded-[11px]' : 'rounded-[13px]';
  const textClass = size === 'sm' ? 'text-[10px]' : 'text-[11px]';

  const firstEmptyIndex = filled + (hasPartial ? 1 : 0);

  return (
    <div className={cn('flex gap-1.5', className)}>
      {Array.from({ length: targetGlasses }, (_, i) => {
        if (i < filled) {
          return (
            <button
              key={i}
              type="button"
              disabled={removeDisabled}
              onClick={onRemoveNewest}
              title={t('hydration.remove_glass_aria')}
              aria-label={t('hydration.remove_glass_aria')}
              className={cn(
                'group focus-visible:ring-primary relative flex-1 place-items-center overflow-hidden focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-60',
                heightClass,
                radiusClass,
              )}
              style={{ background: 'var(--color-water)' }}
            >
              <span
                className={cn(
                  'font-bold text-white opacity-85 group-hover:opacity-0 group-focus-visible:opacity-0',
                  textClass,
                )}
              >
                {glassMl}
              </span>
              <Minus
                className="absolute inset-0 m-auto size-4 text-white opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100"
                strokeWidth={3}
              />
            </button>
          );
        }

        if (i === filled && hasPartial) {
          return (
            <div
              key={i}
              className={cn('grid flex-1 place-items-center', heightClass, radiusClass)}
              style={{ background: 'color-mix(in oklab, var(--color-water) 55%, transparent)' }}
            >
              <span className={cn('font-bold text-white opacity-90', textClass)}>{partialMl}</span>
            </div>
          );
        }

        if (i === firstEmptyIndex) {
          return (
            <button
              key={i}
              type="button"
              disabled={addDisabled}
              onClick={onAdd}
              title={t('hydration.add_glass_aria', { amount: glassMl })}
              aria-label={t('hydration.add_glass_aria', { amount: glassMl })}
              className={cn(
                'border-border-strong bg-muted text-text-2 hover:text-foreground focus-visible:ring-primary grid flex-1 place-items-center border border-dashed transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-60',
                heightClass,
                radiusClass,
              )}
            >
              <Plus className="size-[17px]" strokeWidth={2.2} />
            </button>
          );
        }

        return (
          <div
            key={i}
            aria-hidden
            className={cn(
              'border-border-strong bg-muted flex-1 border border-dashed',
              heightClass,
              radiusClass,
            )}
          />
        );
      })}
    </div>
  );
}
