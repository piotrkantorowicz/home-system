import { cn } from '@shared/lib/utils';
import { AlertTriangle, CheckCircle2, Info, X, XCircle } from 'lucide-react';
import { useEffect, useEffectEvent, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

import type { ToastItem, ToastVariant } from '@shared/context/ToastContext';
import type { LucideIcon } from 'lucide-react';

interface VariantConfig {
  icon: LucideIcon;
  containerClass: string;
  iconClass: string;
  progressClass: string;
  role: 'alert' | 'status';
  ariaLive: 'assertive' | 'polite';
}

const VARIANT_CONFIG: Record<ToastVariant, VariantConfig> = {
  success: {
    icon: CheckCircle2,
    containerClass: 'border-green-200/80 bg-white/95 dark:border-green-800/60 dark:bg-green-950/90',
    iconClass: 'text-green-500 dark:text-green-400',
    progressClass: 'bg-green-500',
    role: 'status',
    ariaLive: 'polite',
  },
  error: {
    icon: XCircle,
    containerClass: 'border-red-200/80 bg-white/95 dark:border-red-800/60 dark:bg-red-950/90',
    iconClass: 'text-red-500 dark:text-red-400',
    progressClass: 'bg-red-500',
    role: 'alert',
    ariaLive: 'assertive',
  },
  warning: {
    icon: AlertTriangle,
    containerClass: 'border-amber-200/80 bg-white/95 dark:border-amber-800/60 dark:bg-amber-950/90',
    iconClass: 'text-amber-500 dark:text-amber-400',
    progressClass: 'bg-amber-500',
    role: 'alert',
    ariaLive: 'assertive',
  },
  info: {
    icon: Info,
    containerClass: 'border-blue-200/80 bg-white/95 dark:border-blue-800/60 dark:bg-blue-950/90',
    iconClass: 'text-blue-500 dark:text-blue-400',
    progressClass: 'bg-blue-500',
    role: 'status',
    ariaLive: 'polite',
  },
};

const EXIT_DURATION_MS = 250;

interface ToastItemProps {
  toast: ToastItem;
  onDismiss: (id: string) => void;
}

function ToastItemComponent({ toast, onDismiss }: ToastItemProps) {
  const [exiting, setExiting] = useState(false);
  const dismissedRef = useRef(false);
  const config = VARIANT_CONFIG[toast.variant];
  const Icon = config.icon;

  const handleDismiss = () => {
    if (dismissedRef.current) return;
    dismissedRef.current = true;
    setExiting(true);
    setTimeout(() => {
      onDismiss(toast.id);
    }, EXIT_DURATION_MS);
  };

  // Effect Event: the timer always calls the latest handleDismiss without the
  // effect re-arming when the handler identity changes.
  const dismissOnTimeout = useEffectEvent(handleDismiss);

  useEffect(() => {
    if (toast.duration <= 0) return;
    const timer = setTimeout(dismissOnTimeout, toast.duration);
    return () => {
      clearTimeout(timer);
    };
  }, [toast.duration]);

  return (
    <div
      role={config.role}
      aria-live={config.ariaLive}
      className={cn(
        'relative flex w-80 items-start gap-3 overflow-hidden rounded-xl border p-4 shadow-lg backdrop-blur-sm',
        'transition-[transform,opacity] ease-in-out',
        config.containerClass,
        exiting
          ? 'translate-x-4 opacity-0 duration-[250ms]'
          : 'animate-toast-slide-in translate-x-0 opacity-100',
      )}
    >
      <Icon className={cn('mt-0.5 h-5 w-5 shrink-0', config.iconClass)} aria-hidden />

      <div className="flex-1">
        <p className="dark:text-foreground/90 text-sm leading-snug font-medium text-slate-700">
          {toast.message}
        </p>
        {toast.action ? (
          <button
            type="button"
            onClick={() => {
              toast.action?.onClick();
              handleDismiss();
            }}
            className="border-border text-foreground hover:bg-muted mt-2 h-7 rounded-[9px] border px-2.5 text-xs font-bold transition-colors"
          >
            {toast.action.label}
          </button>
        ) : null}
      </div>

      <button
        type="button"
        onClick={handleDismiss}
        aria-label="Dismiss notification"
        className="dark:text-muted-foreground dark:hover:text-foreground focus-visible:ring-ring -mt-1 -mr-1 rounded-md p-1 text-slate-400 transition-colors hover:text-slate-600 focus-visible:ring-2 focus-visible:outline-none"
      >
        <X className="h-4 w-4" />
      </button>

      {/* Auto-dismiss progress bar */}
      {toast.duration > 0 && (
        <div
          aria-hidden
          className={cn('absolute right-0 bottom-0 left-0 h-0.5 origin-left', config.progressClass)}
          style={{ animation: `toast-progress ${String(toast.duration)}ms linear forwards` }}
        />
      )}
    </div>
  );
}

export interface ToastContainerProps {
  toasts: ToastItem[];
  onDismiss: (id: string) => void;
}

export function ToastContainer({ toasts, onDismiss }: ToastContainerProps) {
  if (toasts.length === 0) return null;

  return createPortal(
    <div
      aria-label="Notifications"
      className="pointer-events-none fixed right-4 bottom-4 z-[9999] flex flex-col gap-2"
    >
      {toasts.map((toast) => (
        <div key={toast.id} className="pointer-events-auto">
          <ToastItemComponent toast={toast} onDismiss={onDismiss} />
        </div>
      ))}
    </div>,
    document.body,
  );
}
