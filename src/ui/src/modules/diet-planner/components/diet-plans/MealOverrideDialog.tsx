import {
  type OverrideMealEntryRequest,
  useOverrideMeal,
} from '@modules/diet-planner/api/hooks/useMeals';
import { useProducts } from '@modules/diet-planner/api/hooks/useProducts';
import { useRecipes } from '@modules/diet-planner/api/hooks/useRecipes';
import { Button } from '@shared/components/ui/Button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@shared/components/ui/Dialog';
import { Input } from '@shared/components/ui/Input';
import { Label } from '@shared/components/ui/Label';
import { useToast } from '@shared/context/ToastContext';
import { Loader2, Plus, X } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

export interface MealOverrideDialogProps {
  open: boolean;
  onClose: () => void;
  mealEntryId: string;
}

interface ProductRow {
  id: string;
  productId: string;
  productName: string;
  amount: string;
  unit: string;
}

const newRow = (): ProductRow => ({
  id: crypto.randomUUID(),
  productId: '',
  productName: '',
  amount: '',
  unit: 'g',
});

export function MealOverrideDialog({ open, onClose, mealEntryId }: MealOverrideDialogProps) {
  const { t } = useTranslation();
  const toast = useToast();
  const overrideMutation = useOverrideMeal();

  const [recipeSearch, setRecipeSearch] = useState('');
  const [recipeId, setRecipeId] = useState<string>('');
  const [recipeName, setRecipeName] = useState<string>('');
  const [productRows, setProductRows] = useState<ProductRow[]>([]);

  const { data: recipesData } = useRecipes({ search: recipeSearch, pageSize: 20 });
  const recipes =
    (recipesData as { items?: { id: string; name: string }[] } | undefined)?.items ?? [];

  const productSearchTerm = productRows.find((r) => !r.productId)?.productName ?? '';
  const { data: productsData } = useProducts({
    search: productSearchTerm,
    onlyMine: false,
    page: 1,
    pageSize: 50,
  });
  const products = productsData?.items ?? [];

  const isValid =
    recipeId !== '' || productRows.some((r) => r.productId !== '' && Number(r.amount) > 0);

  const reset = () => {
    setRecipeSearch('');
    setRecipeId('');
    setRecipeName('');
    setProductRows([]);
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  const handleSubmit = async () => {
    const body: OverrideMealEntryRequest = {
      actualRecipeId: recipeId === '' ? null : recipeId,
      actualProducts: productRows
        .filter((r) => r.productId !== '' && Number(r.amount) > 0)
        .map((r) => ({
          productId: r.productId,
          amount: Number(r.amount),
          unit: r.unit,
        })),
    };

    try {
      await overrideMutation.mutateAsync({ id: mealEntryId, data: body });
      toast.success(t('calendar.meal_action_success.override'));
      handleClose();
    } catch {
      toast.error(t('calendar.meal_action_error.override'));
    }
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(v) => {
        if (!v) handleClose();
      }}
    >
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{t('override_dialog.title')}</DialogTitle>
          <DialogDescription className="sr-only">{t('override_dialog.title')}</DialogDescription>
        </DialogHeader>

        <div className="space-y-5">
          <section className="space-y-2">
            <Label>{t('override_dialog.replace_recipe.heading')}</Label>
            <Input
              placeholder={t('override_dialog.replace_recipe.placeholder')}
              list="override-recipe-list"
              value={recipeName}
              onChange={(e) => {
                setRecipeName(e.target.value);
                setRecipeSearch(e.target.value);
                const match = recipes.find((r) => r.name === e.target.value);
                setRecipeId(match?.id ?? '');
              }}
            />
            <datalist id="override-recipe-list">
              {recipes.map((r) => (
                <option key={r.id} value={r.name} />
              ))}
            </datalist>
          </section>

          <section className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>{t('override_dialog.add_products.heading')}</Label>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  setProductRows((rows) => [...rows, newRow()]);
                }}
              >
                <Plus className="mr-1 h-4 w-4" />
                {t('override_dialog.add_products.add_button')}
              </Button>
            </div>
            {productRows.map((row, idx) => (
              <div key={row.id} className="flex items-start gap-2">
                <div className="flex-1">
                  <Input
                    placeholder={t('override_dialog.product')}
                    list={`override-product-list-${String(idx)}`}
                    value={row.productName}
                    onChange={(e) => {
                      const value = e.target.value;
                      setProductRows((rows) =>
                        rows.map((r) =>
                          r.id === row.id
                            ? {
                                ...r,
                                productName: value,
                                productId: products.find((p) => p.name === value)?.id ?? '',
                              }
                            : r,
                        ),
                      );
                    }}
                  />
                  <datalist id={`override-product-list-${String(idx)}`}>
                    {products.map((p) => (
                      <option key={p.id} value={p.name} />
                    ))}
                  </datalist>
                </div>
                <Input
                  type="number"
                  step="0.1"
                  min="0"
                  className="w-20"
                  placeholder={t('override_dialog.amount')}
                  value={row.amount}
                  onChange={(e) => {
                    setProductRows((rows) =>
                      rows.map((r) => (r.id === row.id ? { ...r, amount: e.target.value } : r)),
                    );
                  }}
                />
                <Input
                  className="w-16"
                  placeholder={t('override_dialog.unit')}
                  value={row.unit}
                  onChange={(e) => {
                    setProductRows((rows) =>
                      rows.map((r) => (r.id === row.id ? { ...r, unit: e.target.value } : r)),
                    );
                  }}
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  onClick={() => {
                    setProductRows((rows) => rows.filter((r) => r.id !== row.id));
                  }}
                  aria-label="remove"
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </section>

          {!isValid && (
            <p className="text-muted-foreground text-xs">{t('override_dialog.validation.empty')}</p>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleClose} disabled={overrideMutation.isPending}>
            {t('override_dialog.cancel')}
          </Button>
          <Button
            onClick={() => void handleSubmit()}
            disabled={!isValid || overrideMutation.isPending}
          >
            {overrideMutation.isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                {t('common.saving')}
              </>
            ) : (
              t('override_dialog.submit')
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
