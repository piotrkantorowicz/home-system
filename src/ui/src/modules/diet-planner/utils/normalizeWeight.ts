/**
 * The OpenAPI spec types decimal as `number | string`. This coerces it
 * to a stable JS number for arithmetic and formatting.
 */
export function normalizeWeight(value: number | string): number {
  return typeof value === 'number' ? value : Number(value);
}
