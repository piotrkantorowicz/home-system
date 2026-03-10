import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { api } from '../client';
import type { components } from '../generated/schema';

type Product = {
  id: string;
  name: string;
  caloriesPer100g: number;
  proteinPer100g: number;
  carbsPer100g: number;
  fatPer100g: number;
  fiberPer100g?: number | null;
  defaultUnit: string;
  densityGramsPerMl?: number | null;
  gramPerPiece?: number | null;
  isOwner: boolean;
  createdAt: string;
};

type ProductsResponse = {
  items: Product[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

interface ProductsQueryParams {
  search?: string;
  onlyMine?: boolean;
  page?: number;
  pageSize?: number;
}

export function useProducts(params: ProductsQueryParams = {}) {
  const { search = '', onlyMine = false, page = 1, pageSize = 50 } = params;

  return useQuery({
    queryKey: ['products', { search, onlyMine, page, pageSize }],
    queryFn: async (): Promise<ProductsResponse> => {
      const response = await api.GET('/api/v1/products', {
        params: {
          query: { search, onlyMine, page, pageSize },
        },
      });

      if (response.error) {
        throw new Error('Failed to fetch products');
      }

      return response.data as ProductsResponse;
    },
    placeholderData: keepPreviousData,
  });
}

export function useProduct(id: string) {
  return useQuery({
    queryKey: ['products', id],
    queryFn: async (): Promise<Product> => {
      const response = await api.GET('/api/v1/products/{id}', {
        params: {
          path: { id },
        },
      });

      if (response.error) {
        throw new Error('Failed to fetch product');
      }

      return response.data as Product;
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
      queryClient.invalidateQueries({ queryKey: ['products'] });
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
      queryClient.invalidateQueries({ queryKey: ['products'] });
      queryClient.invalidateQueries({ queryKey: ['products', id] });
    },
  });
}

export function useDeleteProduct() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, permanent = false }: { id: string; permanent?: boolean }) => {
      const response = await api.DELETE('/api/v1/products/{id}', {
        params: {
          path: { id },
          query: { permanent },
        },
      });

      if (response.error) {
        throw new Error('Failed to delete product');
      }

      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] });
    },
  });
}
