import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';

interface ProfileDto {
  id: string;
  userId: string;
  dateOfBirth: string | null;
  gender: string | null;
  heightCm: number | null;
  currentWeightKg: number | null;
  targetWeightKg: number | null;
  activityLevel: string | null;
  createdAt: string;
  updatedAt: string | null;
}

interface ProfileRequest {
  dateOfBirth?: string | null;
  gender?: string | null;
  heightCm?: number | null;
  currentWeightKg?: number | null;
  targetWeightKg?: number | null;
  activityLevel?: string | null;
}

interface ProfileApiResponse {
  data?: ProfileDto;
  error?: unknown;
}

interface ProfileUpdateApiResponse {
  data?: string;
  error?: unknown;
}

export function useProfile() {
  return useQuery({
    queryKey: ['profile'],
    queryFn: async (): Promise<ProfileDto | null> => {
      // REASON: /api/v1/profile is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).GET('/api/v1/profile')) as ProfileApiResponse;

      if (response.error) {
        throw new Error('Failed to fetch profile');
      }

      return response.data ?? null;
    },
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: ProfileRequest) => {
      // REASON: /api/v1/profile is not yet in the generated openapi schema — regenerate schema to remove this cast
      // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      const response = (await (api as any).PUT('/api/v1/profile', {
        body: data,
      })) as ProfileUpdateApiResponse;

      if (response.error) {
        throw new Error('Failed to update profile');
      }

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}
