/** `Household.Contracts.Events.MemberJoined, Household.Contracts` → `MemberJoined`. */
export function shortEventType(eventType: string): string {
  const typeName = eventType.split(',')[0] ?? eventType;
  return typeName.slice(typeName.lastIndexOf('.') + 1) || eventType;
}

export function formatDateTime(iso: string | null, locale: string): string {
  return iso ? new Date(iso).toLocaleString(locale) : '—';
}
