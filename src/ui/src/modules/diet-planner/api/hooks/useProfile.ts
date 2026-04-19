import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { api } from '../client';
import { queryKeys } from '../queryKeys';

import type { components } from '../generated/schema';

export type UserProfileDto = components['schemas']['UserProfileDto'];
export type ProfileRequest = components['schemas']['ProfileRequest'];

export function useProfile() {
  return useQuery({
    queryKey: queryKeys.profile.detail(),
    queryFn: async (): Promise<UserProfileDto | null> => {
      const response = await api.GET('/api/v1/profile');

      if (response.response.status === 404) {
        return null;
      }

      if (!response.response.ok) {
        throw new Error('Failed to fetch profile');
      }

      return response.data ?? null;
    },
  });
}

export function useCreateProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: ProfileRequest) => {
      const response = await api.POST('/api/v1/profile', { body: data });

      if (!response.response.ok) {
        throw new Error('Failed to create profile');
      }

      return response.data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: ProfileRequest) => {
      const response = await api.PUT('/api/v1/profile', { body: data });

      if (!response.response.ok) {
        throw new Error('Failed to update profile');
      }
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['profile'] });
    },
  });
}
