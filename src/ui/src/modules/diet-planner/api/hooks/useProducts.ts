import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';

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

export function useProducts(params: ProductsQueryParams = {}) {
  const { search = '', onlyMine = false, page = 1, pageSize = 50 } = params;

  return useQuery({
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
    placeholderData: keepPreviousData,
  });
}

export function useProduct(id: string) {
  return useQuery({
    queryKey: queryKeys.products.detail(id),
    queryFn: async (): Promise<Product> => {
      const response = await api.GET('/api/v1/products/{id}', {
        params: {
          path: { id },
        },
      });

      if (!response.data) {
        throw new Error('Failed to fetch product');
      }

      return mapProduct(response.data);
    },
    enabled: !!id,
  });
}

export function useCreateProduct() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (productData: components['schemas']['CreateProductRequest']) => {
      const response = await api.POST('/api/v1/products', {
        body: productData,
      });

      if (response.error) {
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

      if (response.error) {
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
