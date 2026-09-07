import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { useWaterActions } from './useWaterActions';

const mocks = vi.hoisted(() => ({
  log: vi.fn(),
  remove: vi.fn(),
  success: vi.fn(),
  error: vi.fn(),
}));
vi.mock('./useHydration', () => ({
  useLogWaterIntake: () => ({ mutateAsync: mocks.log }),
  useDeleteWaterIntake: () => ({ mutateAsync: mocks.remove }),
}));
vi.mock('@shared/context/ToastContext', () => ({
  useToast: () => ({ success: mocks.success, error: mocks.error }),
}));
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (key: string) => key }) }));
beforeEach(() => vi.resetAllMocks());

describe('useWaterActions', () => {
  it('blocks duplicate pending amounts while allowing other amounts and settling both', async () => {
    let finish: (id: string) => void = () => undefined;
    mocks.log.mockImplementation(({ amountMl }: { amountMl: number }) =>
      amountMl === 250
        ? new Promise<string>((resolve) => {
            finish = resolve;
          })
        : Promise.resolve('large'),
    );
    const { result } = renderHook(() => useWaterActions('2026-09-03'));
    act(() => {
      result.current.add(250);
      result.current.add(250);
      result.current.add(500);
    });
    expect(mocks.log).toHaveBeenCalledTimes(2);
    await waitFor(() => {
      expect(result.current.pendingKeys.has('add-500')).toBe(false);
    });
    expect(result.current.pendingKeys.has('add-250')).toBe(true);
    act(() => {
      finish('small');
    });
    await waitFor(() => {
      expect(result.current.pendingKeys.size).toBe(0);
    });
    expect(result.current.pendingKeys.size).toBe(0);
    expect(mocks.success).toHaveBeenCalledTimes(2);
  });
  it('undo deletes exactly the returned ID and reports deletion failure', async () => {
    mocks.log.mockResolvedValue('new-entry');
    mocks.remove.mockRejectedValue(new Error('offline'));
    const { result } = renderHook(() => useWaterActions('2026-09-03'));
    act(() => {
      result.current.add(250);
    });
    await waitFor(() => {
      expect(mocks.success).toHaveBeenCalled();
    });
    const options = mocks.success.mock.calls[0]?.[1] as { action: { onClick: () => void } };
    act(() => {
      options.action.onClick();
    });
    await waitFor(() => {
      expect(mocks.error).toHaveBeenCalledWith('hydration.delete_error');
    });
    expect(mocks.remove).toHaveBeenCalledWith({ id: 'new-entry', date: '2026-09-03' });
  });
});
