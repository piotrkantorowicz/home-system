import { Sparkles } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { OPEN_ASSISTANT_EVENT } from './AssistantPanel';
import { useAssistantStatus } from './assistantStatus';

export interface AssistantButtonProps {
  className?: string;
  /** Show the text label next to the icon (sidebar); icon only otherwise (mobile header). */
  withLabel?: boolean;
}

/** Opens the {@link AssistantPanel}; hidden while the assistant is switched off. */
export function AssistantButton({ className, withLabel = false }: AssistantButtonProps) {
  const { t } = useTranslation();
  const { data: status } = useAssistantStatus();

  if (!status) return null;

  return (
    <button
      type="button"
      aria-label={withLabel ? undefined : t('assistant.open')}
      className={className}
      onClick={() => {
        window.dispatchEvent(new Event(OPEN_ASSISTANT_EVENT));
      }}
    >
      <Sparkles className="size-4 shrink-0" strokeWidth={1.9} />
      {withLabel && (
        <span className="min-w-0 flex-1 truncate text-left">{t('assistant.title')}</span>
      )}
    </button>
  );
}
