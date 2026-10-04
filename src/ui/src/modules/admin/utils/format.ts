import { formatDayRange, formatTime } from '@shared/lib/format';

/** `Household.Contracts.Events.MemberJoined, Household.Contracts` → `MemberJoined`. */
export function shortEventType(eventType: string): string {
  const typeName = eventType.split(',')[0] ?? eventType;
  return typeName.slice(typeName.lastIndexOf('.') + 1) || eventType;
}

/** `MemberJoined` → `Member joined`: readable without a translation for every event. */
export function eventDisplayName(eventType: string): string {
  const words = shortEventType(eventType).replace(/([a-z0-9])([A-Z])/g, '$1 $2');
  return words.charAt(0).toUpperCase() + words.slice(1).toLowerCase();
}

/** `DietPlanner` → `Diet planner`; modules the table does not know are split on capitals. */
export function sourceDisplayName(module: string): string {
  const words = module.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
  return words.charAt(0).toUpperCase() + words.slice(1).toLowerCase();
}

/** Local time of an instant: "2 Oct, 20:40" (day first, never UTC). */
export function formatDateTime(iso: string | null, locale: string): string {
  if (!iso) return '—';
  const day = formatDayRange(iso, iso, locale).split(' – ')[0] ?? '';
  return `${day}, ${formatTime(iso, locale)}`;
}
