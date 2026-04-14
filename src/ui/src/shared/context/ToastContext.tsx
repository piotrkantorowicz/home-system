import { ToastContainer } from '@shared/components/ui/Toast';
import { createContext, useCallback, useContext, useReducer, useRef } from 'react';

import type { ReactNode } from 'react';

export type ToastVariant = 'success' | 'error' | 'warning' | 'info';

export interface ToastItem {
  id: string;
  variant: ToastVariant;
  message: string;
  duration: number;
}

type ToastAction = { type: 'ADD'; toast: ToastItem } | { type: 'REMOVE'; id: string };

function toastReducer(
  state: { toasts: ToastItem[] },
  action: ToastAction,
): { toasts: ToastItem[] } {
  switch (action.type) {
    case 'ADD':
      return { toasts: [...state.toasts, action.toast] };
    case 'REMOVE':
      return { toasts: state.toasts.filter((t) => t.id !== action.id) };
  }
}

export interface ToastContextValue {
  success: (message: string, duration?: number) => void;
  error: (message: string, duration?: number) => void;
  warning: (message: string, duration?: number) => void;
  info: (message: string, duration?: number) => void;
  dismiss: (id: string) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(toastReducer, { toasts: [] });
  const counterRef = useRef(0);

  const dismiss = useCallback((id: string) => {
    dispatch({ type: 'REMOVE', id });
  }, []);

  const add = useCallback((variant: ToastVariant, message: string, duration = 4000) => {
    counterRef.current += 1;
    const id = `toast-${String(counterRef.current)}`;
    dispatch({ type: 'ADD', toast: { id, variant, message, duration } });
  }, []);

  const value: ToastContextValue = {
    success: (msg, dur) => {
      add('success', msg, dur);
    },
    error: (msg, dur) => {
      add('error', msg, dur);
    },
    warning: (msg, dur) => {
      add('warning', msg, dur);
    },
    info: (msg, dur) => {
      add('info', msg, dur);
    },
    dismiss,
  };

  return (
    <ToastContext.Provider value={value}>
      {children}
      <ToastContainer toasts={state.toasts} onDismiss={dismiss} />
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return ctx;
}
