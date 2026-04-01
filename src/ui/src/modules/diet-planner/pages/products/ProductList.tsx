import { useProducts, useDeleteProduct } from '@modules/diet-planner/api/hooks/useProducts';
import { unitLabel } from '@modules/diet-planner/unitLabel';
import {
  Button,
  Input,
  Pagination,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@shared/components/ui';
import { Badge } from '@shared/components/ui/Badge';
import { Plus, Search, Edit, Trash2, Eye } from 'lucide-react';
import { useState, useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

export default function ProductList() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [onlyMine, setOnlyMine] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [productToDelete, setProductToDelete] = useState<string | null>(null);

  const { data, isLoading, error } = useProducts({
    search: debouncedSearch,
    onlyMine,
    page,
    pageSize,
  });

  const deleteMutation = useDeleteProduct();

  const searchTimerRef = useRef<ReturnType<typeof setTimeout>>(undefined);
  useEffect(
    () => () => {
      clearTimeout(searchTimerRef.current);
    },
    [],
  );

  const handleSearchChange = (value: string) => {
    setSearch(value);
    clearTimeout(searchTimerRef.current);
    searchTimerRef.current = setTimeout(() => {
      setDebouncedSearch(value);
      setPage(1);
    }, 300);
  };

  const handleDelete = async () => {
    if (productToDelete) {
      await deleteMutation.mutateAsync({ id: productToDelete });
      setDeleteDialogOpen(false);
      setProductToDelete(null);
    }
  };

  const openDeleteDialog = (id: string) => {
    setProductToDelete(id);
    setDeleteDialogOpen(true);
  };

  return (
    <div className="animate-fade-in-up p-8 lg:p-10">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="mb-2 text-4xl font-bold tracking-tight">{t('products.title')}</h1>
          <p className="text-muted-foreground text-[0.95rem]">{t('products.subtitle')}</p>
        </div>
        <Button asChild>
          <Link to="/diet-planner/products/new">
            <Plus className="mr-2 h-4 w-4" />
            {t('products.add_product')}
          </Link>
        </Button>
      </div>

      <div className="mb-6 flex items-center gap-4">
        <div className="relative max-w-sm flex-1">
          <Search className="text-muted-foreground absolute top-1/2 left-3.5 h-4 w-4 -translate-y-1/2" />
          <Input
            placeholder={t('products.search_placeholder')}
            value={search}
            onChange={(e) => {
              handleSearchChange(e.target.value);
            }}
            className="pl-10"
          />
        </div>
        <Button
          variant={onlyMine ? 'default' : 'outline'}
          onClick={() => {
            setOnlyMine(!onlyMine);
            setPage(1);
          }}
        >
          {onlyMine ? t('products.showing_my_products') : t('products.show_only_mine')}
        </Button>
      </div>

      {error && (
        <div className="border-destructive/30 bg-destructive/5 text-destructive animate-scale-in mb-4 rounded-xl border p-4">
          {t('common.error')}: {error.message}
        </div>
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="text-muted-foreground text-lg">{t('common.loading')}</div>
        </div>
      ) : (
        <>
          {data && (
            <Pagination
              page={page}
              pageSize={pageSize}
              totalCount={data.totalCount}
              onPageChange={setPage}
              onPageSizeChange={(size) => {
                setPageSize(size);
                setPage(1);
              }}
            />
          )}
          {data?.items.length === 0 ? (
            <div className="animate-fade-in flex flex-col items-center justify-center py-16 text-center">
              <p className="mb-2 text-xl font-semibold">{t('products.no_products_found')}</p>
              <p className="text-muted-foreground mb-6">
                {debouncedSearch ? t('products.adjust_search') : t('products.start_creating')}
              </p>
              {!debouncedSearch && (
                <Button asChild>
                  <Link to="/diet-planner/products/new">
                    <Plus className="mr-2 h-4 w-4" />
                    {t('products.add_first_product')}
                  </Link>
                </Button>
              )}
            </div>
          ) : (
            <div className="overflow-hidden rounded-xl border">
              <Table>
                <TableHeader>
                  <TableRow className="bg-muted/30">
                    <TableHead>{t('products.table.name')}</TableHead>
                    <TableHead>{t('products.table.calories')}</TableHead>
                    <TableHead>{t('products.table.protein')}</TableHead>
                    <TableHead>{t('products.table.carbs')}</TableHead>
                    <TableHead>{t('products.table.fat')}</TableHead>
                    <TableHead>{t('products.table.fiber')}</TableHead>
                    <TableHead>{t('products.table.unit')}</TableHead>
                    <TableHead>{t('products.table.owner')}</TableHead>
                    <TableHead className="text-right">{t('common.actions')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data?.items.map((product) => (
                    <TableRow key={product.id}>
                      <TableCell className="font-medium">{product.name}</TableCell>
                      <TableCell>{(product.caloriesPer100g ?? 0).toFixed(1)} kcal</TableCell>
                      <TableCell>{(product.proteinPer100g ?? 0).toFixed(1)}g</TableCell>
                      <TableCell>{(product.carbsPer100g ?? 0).toFixed(1)}g</TableCell>
                      <TableCell>{(product.fatPer100g ?? 0).toFixed(1)}g</TableCell>
                      <TableCell>{(product.fiberPer100g ?? 0).toFixed(1)}g</TableCell>
                      <TableCell>
                        <Badge variant="secondary">{unitLabel(product.defaultUnit, t)}</Badge>
                      </TableCell>
                      <TableCell>
                        {product.isOwner ? (
                          <Badge variant="default">{t('common.you')}</Badge>
                        ) : (
                          <Badge variant="outline">{t('common.shared')}</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-1">
                          <Link
                            to={`/diet-planner/products/${product.id}`}
                            aria-label={t('common.view')}
                          >
                            <Button variant="ghost" size="icon" className="hover:text-primary">
                              <Eye className="h-4 w-4" />
                            </Button>
                          </Link>
                          {product.isOwner && (
                            <>
                              <Link
                                to={`/diet-planner/products/${product.id}/edit`}
                                aria-label={t('common.edit')}
                              >
                                <Button variant="ghost" size="icon" className="hover:text-primary">
                                  <Edit className="h-4 w-4" />
                                </Button>
                              </Link>
                              <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => {
                                  openDeleteDialog(product.id);
                                }}
                                className="hover:text-destructive"
                                aria-label={t('common.delete')}
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            </>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </>
      )}

      <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('products.delete_dialog.title')}</DialogTitle>
            <DialogDescription>{t('products.delete_dialog.description')}</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setDeleteDialogOpen(false);
              }}
            >
              {t('common.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                void handleDelete();
              }}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending
                ? t('products.delete_dialog.deleting')
                : t('products.delete_dialog.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
