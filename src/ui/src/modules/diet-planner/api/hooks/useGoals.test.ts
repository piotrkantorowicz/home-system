import { act, renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';

import { useCreateGoals, useGoals } from './useGoals';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

it('accepts an empty 201 response and refreshes goals', async () => {
  const target = {
    id: 'goal',
    dailyCalorieTarget: 2150,
    proteinGrams: 140,
    carbsGrams: 240,
    fatGrams: 70,
    fiberGrams: 30,
    createdAt: '',
    updatedAt: null,
  };
  let created = false;
  server.use(
    http.get('http://localhost:5050/api/v1/goals', () =>
      HttpResponse.json(created ? target : null),
    ),
    http.post('http://localhost:5050/api/v1/goals', () => {
      created = true;
      return new HttpResponse(null, { status: 201 });
    }),
  );
  const wrapper = createWrapper();
  const { result } = renderHook(() => ({ query: useGoals(), create: useCreateGoals() }), {
    wrapper,
  });
  await waitFor(() => {
    expect(result.current.query.isSuccess).toBe(true);
  });
  await act(async () => {
    await result.current.create.mutateAsync(target);
  });
  await waitFor(() => {
    expect(result.current.query.data?.dailyCalorieTarget).toBe(2150);
  });
  expect(result.current.create.isSuccess).toBe(true);
});
