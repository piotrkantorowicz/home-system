export interface ProductFixture {
  id: string;
  name: string;
  caloriesPer100g: number;
  proteinPer100g: number;
  carbsPer100g: number;
  fatPer100g: number;
  fiberPer100g: number | null;
  defaultUnit: string;
  densityGramsPerMl: number | null;
  gramPerPiece: number | null;
  isOwner: boolean;
  createdAt: string;
}

let counter = 0;

export function productFactory(overrides: Partial<ProductFixture> = {}): ProductFixture {
  counter += 1;
  return {
    id: `product-${String(counter)}-0000-0000-0000-000000000000`,
    name: `Product ${String(counter)}`,
    caloriesPer100g: 100,
    proteinPer100g: 10,
    carbsPer100g: 15,
    fatPer100g: 5,
    fiberPer100g: null,
    defaultUnit: 'g',
    densityGramsPerMl: null,
    gramPerPiece: null,
    isOwner: true,
    createdAt: new Date().toISOString(),
    ...overrides,
  };
}
