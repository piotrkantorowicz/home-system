import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';

export function useListLocation() {
  const [params, setParams] = useSearchParams();
  const search = params.get('search') ?? '';
  const [debouncedSearch, setDebouncedSearch] = useState(search);
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
    }, 300);
    return () => {
      clearTimeout(timer);
    };
  }, [search]);

  function update(values: Record<string, string | number | null>, replace = false) {
    setParams(
      (previous) => {
        const next = new URLSearchParams(previous);
        for (const [key, value] of Object.entries(values)) {
          if (value === null || value === '') next.delete(key);
          else next.set(key, String(value));
        }
        return next;
      },
      { replace },
    );
  }

  const rawPage = Number(params.get('page') ?? 1);
  const rawSize = Number(params.get('pageSize') ?? 25);
  return {
    params,
    search,
    debouncedSearch,
    update,
    page: Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1,
    pageSize: [10, 25, 50, 100].includes(rawSize) ? rawSize : 25,
    setPage: (page: number) => {
      update({ page });
    },
    setPageSize: (pageSize: number) => {
      update({ pageSize, page: null });
    },
    setSearch: (search: string) => {
      update({ search, page: null }, true);
    },
  };
}
