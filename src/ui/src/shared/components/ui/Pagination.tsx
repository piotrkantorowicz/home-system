import { useTranslation } from 'react-i18next';

import { Button } from './Button';

const DEFAULT_PAGE_SIZE_OPTIONS = [10, 25, 50, 100];

interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  pageSizeOptions?: number[];
}

export function Pagination({
  page,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = DEFAULT_PAGE_SIZE_OPTIONS,
}: PaginationProps) {
  const { t } = useTranslation();
  const totalPages = Math.ceil(totalCount / pageSize);
  const start = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);

  return (
    <div className="mt-5 flex flex-wrap items-center justify-between gap-4">
      <div className="flex items-center gap-2">
        <span className="text-muted-foreground text-sm">{t('common.rows_per_page')}</span>
        <select
          value={pageSize}
          onChange={(e) => {
            onPageSizeChange(Number(e.target.value));
            onPageChange(1);
          }}
          className="border-input bg-background focus:ring-ring h-8 rounded-md border px-2 text-sm focus:ring-1 focus:outline-none"
        >
          {pageSizeOptions.map((opt) => (
            <option key={opt} value={opt}>
              {opt}
            </option>
          ))}
        </select>
      </div>

      <div className="flex items-center gap-4">
        {totalCount > 0 && (
          <p className="text-muted-foreground text-sm">
            {t('common.showing_range', { start, end, total: totalCount })}
          </p>
        )}
        <div className="flex gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              onPageChange(Math.max(1, page - 1));
            }}
            disabled={page <= 1}
          >
            {t('common.previous')}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              onPageChange(page + 1);
            }}
            disabled={page >= totalPages}
          >
            {t('common.next')}
          </Button>
        </div>
      </div>
    </div>
  );
}
