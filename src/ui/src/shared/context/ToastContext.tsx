import { ToastContainer } from '@shared/components/ui/Toast';
import { createContext, useCallback, useContext, useReducer, useRef } from 'react';

import type { ReactNode } from 'react';

export type ToastVariant = 'success' | 'error' | 'warning' | 'info';

export interface ToastAction {
  label: string;
  onClick: () => void;
}

export interface ToastItem {
  id: string;
  variant: ToastVariant;
  message: string;
  duration: number;
  action?: ToastAction;
}

type ToastReducerAction = { type: 'ADD'; toast: ToastItem } | { type: 'REMOVE'; id: string };

function toastReducer(
  state: { toasts: ToastItem[] },
  action: ToastReducerAction,
): { toasts: ToastItem[] } {
  switch (action.type) {
    case 'ADD':
      return { toasts: [...state.toasts, action.toast] };
    case 'REMOVE':
      return { toasts: state.toasts.filter((t) => t.id !== action.id) };
  }
}

export interface ToastOptions {
  duration?: number;
  action?: ToastAction;
}

export interface ToastContextValue {
  success: (message: string, options?: ToastOptions) => void;
  error: (message: string, options?: ToastOptions) => void;
  warning: (message: string, options?: ToastOptions) => void;
  info: (message: string, options?: ToastOptions) => void;
  dismiss: (id: string) => void;
}

// eslint-disable-next-line react-refresh/only-export-components
export const ToastContext = createContext<ToastContextValue | null>(null);

// eslint-disable-next-line react-refresh/only-export-components
export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within a ToastProvider');
  return ctx;
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(toastReducer, { toasts: [] });
  const counterRef = useRef(0);

  const dismiss = useCallback((id: string) => {
    dispatch({ type: 'REMOVE', id });
  }, []);

  const add = useCallback((variant: ToastVariant, message: string, options?: ToastOptions) => {
    counterRef.current += 1;
    const id = `toast-${String(counterRef.current)}`;
    const duration = options?.duration ?? 4000;
    dispatch({
      type: 'ADD',
      toast: { id, variant, message, duration, ...(options?.action ? { action: options.action } : {}) },
    });
  }, []);

  const value: ToastContextValue = {
    success: (msg, opts) => {
      add('success', msg, opts);
    },
    error: (msg, opts) => {
      add('error', msg, opts);
    },
    warning: (msg, opts) => {
      add('warning', msg, opts);
    },
    info: (msg, opts) => {
      add('info', msg, opts);
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
