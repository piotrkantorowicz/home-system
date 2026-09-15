import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

export interface HydrationConfigDto {
  id: string;
  userId: string;
  dailyWaterTargetMl: number;
  glassSizeMl: number;
  trackWaterIntake: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface WaterIntakeEntryDto {
  id: string;
  amountMl: number;
  timestamp: string;
  note: string | null;
}

export interface WaterIntakeListDto {
  date: string;
  totalMl: number;
  entries: WaterIntakeEntryDto[];
}

interface HydrationConfigApiResponse {
  data?: HydrationConfigDto;
  // REASON: openapi-fetch returns `error` as unknown for endpoints not in generated schema
  error?: unknown;
}

interface WaterIntakeApiResponse {
  data?: WaterIntakeListDto;
  // REASON: openapi-fetch returns `error` as unknown for endpoints not in generated schema
  error?: unknown;
}

interface LogWaterIntakeApiResponse {
  data?: string;
  // REASON: openapi-fetch returns `error` as unknown for endpoints not in generated schema
  error?: unknown;
}

interface DeleteWaterIntakeApiResponse {
  data?: undefined;
  // REASON: openapi-fetch returns `error` as unknown for endpoints not in generated schema
  error?: unknown;
}

interface UpdateHydrationConfigApiResponse {
  data?: HydrationConfigDto;
  // REASON: openapi-fetch returns `error` as unknown for endpoints not in generated schema
  error?: unknown;
}

export interface UpdateHydrationConfigData {
  dailyWaterTargetMl: number;
  glassSizeMl: number;
  trackWaterIntake: boolean;
}

export interface LogWaterIntakeData {
  date: string;
  amountMl: number;
  note?: string;
}

export function hydrationConfigOptions() {
  return queryOptions({
    queryKey: queryKeys.hydration.config(),
    queryFn: async (): Promise<HydrationConfigDto | null> => {
      // REASON: /api/v1/hydration/config is not in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).GET(
        '/api/v1/hydration/config',
      )) as HydrationConfigApiResponse;

      if (response.error) {
        throw new Error('Failed to fetch hydration config');
      }

      return response.data ?? null;
    },
  });
}

export function useHydrationConfig() {
  return useQuery(hydrationConfigOptions());
}

export function useUpdateHydrationConfig() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: UpdateHydrationConfigData) => {
      // REASON: /api/v1/hydration/config is not in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).PUT('/api/v1/hydration/config', {
        body: data,
      })) as UpdateHydrationConfigApiResponse;

      if (response.error) {
        throw new Error('Failed to update hydration config');
      }

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.hydration.config() });
    },
  });
}

export function waterIntakeOptions(date: string) {
  return queryOptions({
    queryKey: queryKeys.hydration.intake(date),
    queryFn: async (): Promise<WaterIntakeListDto | null> => {
      // REASON: /api/v1/hydration/intake is not in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).GET('/api/v1/hydration/intake', {
        params: { query: { date } },
      })) as WaterIntakeApiResponse;

      if (response.error) {
        throw new Error('Failed to fetch water intake');
      }

      return response.data ?? null;
    },
  });
}

export function useWaterIntake(date: string) {
  return useQuery(waterIntakeOptions(date));
}

export function useLogWaterIntake() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: LogWaterIntakeData) => {
      // REASON: /api/v1/hydration/intake is not in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).POST('/api/v1/hydration/intake', {
        body: data,
      })) as LogWaterIntakeApiResponse;

      if (response.error) {
        throw new Error('Failed to log water intake');
      }

      return response.data;
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.hydration.intake(variables.date) });
    },
  });
}

export function useDeleteWaterIntake() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, date }: { id: string; date: string }) => {
      // REASON: /api/v1/hydration/intake/{id} is not in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).DELETE(
        `/api/v1/hydration/intake/${id}`,
      )) as DeleteWaterIntakeApiResponse;

      if (response.error) {
        throw new Error('Failed to delete water intake entry');
      }

      return date;
    },
    onSuccess: (date) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.hydration.intake(date) });
    },
  });
}
