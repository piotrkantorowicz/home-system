import {
  keepPreviousData,
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

interface Product {
  id: string;
  name: string;
  caloriesPer100g: number | null;
  proteinPer100g: number | null;
  carbsPer100g: number | null;
  fatPer100g: number | null;
  fiberPer100g?: number | null;
  defaultUnit: string;
  densityGramsPerMl?: number | null;
  gramPerPiece?: number | null;
  isOwner: boolean;
  visibility: string;
  canEdit: boolean;
  createdAt: string;
}

type ApiProduct = components['schemas']['ProductDto'];

function mapProduct(p: ApiProduct): Product {
  const toNum = (v: null | number | string | undefined): number | null =>
    v === null || v === undefined ? null : Number(v);
  return {
    id: p.id,
    name: p.name,
    caloriesPer100g: toNum(p.calories),
    proteinPer100g: toNum(p.protein),
    carbsPer100g: toNum(p.carbs),
    fatPer100g: toNum(p.fat),
    fiberPer100g: toNum(p.fiber),
    defaultUnit: p.defaultUnit,
    densityGramsPerMl: toNum(p.densityGramsPerMl),
    gramPerPiece: toNum(p.gramPerPiece),
    isOwner: p.isOwner,
    visibility: p.visibility,
    canEdit: p.canEdit,
    createdAt: p.createdAt,
  };
}

interface ProductsResponse {
  items: Product[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

interface ProductsQueryParams {
  search?: string;
  onlyMine?: boolean;
  page?: number;
  pageSize?: number;
}

export function productListOptions(params: ProductsQueryParams = {}) {
  const { search = '', onlyMine = false, page = 1, pageSize = 50 } = params;

  return queryOptions({
    queryKey: queryKeys.products.list({ search, onlyMine, page, pageSize }),
    queryFn: async (): Promise<ProductsResponse> => {
      const response = await api.GET('/api/v1/products', {
        params: {
          query: { Search: search, OnlyMine: onlyMine, Page: page, PageSize: pageSize },
        },
      });

      if (!response.data) {
        throw new Error('Failed to fetch products');
      }

      const raw = response.data as {
        items: ApiProduct[];
        page: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
      };
      return { ...raw, items: raw.items.map(mapProduct) };
    },
  });
}

export function useProducts(params: ProductsQueryParams = {}) {
  return useQuery({ ...productListOptions(params), placeholderData: keepPreviousData });
}

export function productOptions(id: string) {
  return queryOptions({
    queryKey: queryKeys.products.detail(id),
    queryFn: async (): Promise<Product | null> => {
      const response = await api.GET('/api/v1/products/{id}', {
        params: {
          path: { id },
        },
      });

      // A missing product is a valid, expected state — pages render "not found" for null.
      if (response.response.status === 404) {
        return null;
      }

      if (!response.data) {
        throw new Error('Failed to fetch product');
      }

      return mapProduct(response.data);
    },
  });
}

export function useProduct(id: string) {
  return useQuery({ ...productOptions(id), enabled: !!id });
}

export function useCreateProduct() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (productData: components['schemas']['CreateProductRequest']) => {
      const response = await api.POST('/api/v1/products', {
        body: productData,
      });

      if (!response.response.ok) {
        throw new Error('Failed to create product');
      }

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.products.all() });
    },
  });
}

export function useUpdateProduct(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (productData: components['schemas']['UpdateProductRequest']) => {
      const response = await api.PUT('/api/v1/products/{id}', {
        params: {
          path: { id },
        },
        body: productData,
      });

      if (!response.response.ok) {
        throw new Error('Failed to update product');
      }

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.products.all() });
      void queryClient.invalidateQueries({ queryKey: queryKeys.products.detail(id) });
    },
  });
}

export function useDeleteProduct() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id }: { id: string }) => {
      const response = await api.DELETE('/api/v1/products/{id}', {
        params: {
          path: { id },
        },
      });

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.products.all() });
    },
  });
}
