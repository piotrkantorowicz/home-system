/** "Paweł Kantorowicz" → "PK"; a single name gives a single letter ("Zosia" → "Z"). */
export function initials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  const first = words[0]?.charAt(0) ?? '';
  const last = words.length > 1 ? (words[words.length - 1]?.charAt(0) ?? '') : '';
  return (first + last).toLocaleUpperCase();
}
