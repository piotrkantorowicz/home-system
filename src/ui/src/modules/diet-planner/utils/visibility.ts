// Mirrors DietPlanner.Domain Visibility — who can see a recipe or product.
export const VISIBILITIES = ['Private', 'Household', 'Public'] as const;

export type Visibility = (typeof VISIBILITIES)[number];

export const DEFAULT_VISIBILITY: Visibility = 'Household';

export function toVisibility(value: string): Visibility {
  return VISIBILITIES.find((v) => v === value) ?? DEFAULT_VISIBILITY;
}
