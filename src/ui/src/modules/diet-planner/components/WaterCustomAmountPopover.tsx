import { Button, Input, Popover, PopoverContent, PopoverTrigger } from '@shared/components/ui';
import { Minus, Plus, Waves } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

export interface WaterCustomAmountPopoverProps {
  /** Up to three amounts to offer as one-tap presets (e.g. the day's own most-used sizes). */
  presets: number[];
  onAdd: (amountMl: number, note?: string) => void;
  pending?: boolean;
  triggerSize?: 'xl' | 'sm';
}

const STEP_ML = 50;
const DEFAULT_AMOUNT = 330;

/**
 * "Custom amount" as a popover instead of two permanently-expanded inputs
 * plus a button (BUILD_REVIEW.md #fix-water). A stepper, up to three presets,
 * and an optional note.
 */
export function WaterCustomAmountPopover({
  presets,
  onAdd,
  pending = false,
  triggerSize = 'xl',
}: WaterCustomAmountPopoverProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [amount, setAmount] = useState(DEFAULT_AMOUNT);
  const [note, setNote] = useState('');

  function step(delta: number) {
    setAmount((prev) => Math.max(STEP_ML, prev + delta));
  }

  function handleAdd() {
    onAdd(amount, note.trim() || undefined);
    setOpen(false);
    setNote('');
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          size={triggerSize}
          variant="outline"
          className="border-dashed"
          disabled={pending}
        >
          <Plus className="size-4" />
          {t('hydration.custom_trigger')}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto">
        <div className="flex flex-col gap-3">
          <div className="text-12-5px font-bold">{t('hydration.custom_popover_title')}</div>

          <div className="flex flex-wrap items-center gap-2.5">
            <div className="border-border bg-card h-42px rounded-13px flex items-center overflow-hidden border">
              <button
                type="button"
                onClick={() => {
                  step(-STEP_ML);
                }}
                aria-label={t('hydration.custom_step_down_aria')}
                className="text-text-2 hover:bg-muted text-17px flex h-full w-9 items-center justify-center"
              >
                <Minus className="size-3.5" />
              </button>
              <span className="tnum w-60px text-15px text-center font-bold">{amount}</span>
              <button
                type="button"
                onClick={() => {
                  step(STEP_ML);
                }}
                aria-label={t('hydration.custom_step_up_aria')}
                className="text-text-2 hover:bg-muted text-17px flex h-full w-9 items-center justify-center"
              >
                <Plus className="size-3.5" />
              </button>
            </div>
            <span className="text-text-2 text-12px font-semibold">
              {t('hydration.custom_ml_unit')}
            </span>

            {presets.length > 0 ? (
              <div className="flex gap-1.5">
                {presets.map((p) => (
                  <button
                    key={p}
                    type="button"
                    onClick={() => {
                      setAmount(p);
                    }}
                    className="border-border bg-card hover:border-border-strong tnum h-30px rounded-9px text-12px border px-2.5 font-bold"
                  >
                    {p}
                  </button>
                ))}
              </div>
            ) : null}
          </div>

          <div className="flex items-center gap-2">
            <Input
              value={note}
              onChange={(e) => {
                setNote(e.target.value);
              }}
              placeholder={t('hydration.custom_note_placeholder')}
              className="h-38px text-12-5px flex-1"
            />
            <Button
              type="button"
              size="sm"
              onClick={handleAdd}
              disabled={pending}
              style={{ background: 'var(--color-water)' }}
            >
              <Waves className="size-3.5" />
              {t('hydration.add_btn')}
            </Button>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
