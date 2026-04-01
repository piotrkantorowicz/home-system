export function createSampleImportData() {
  const ts = Date.now();
  const productName = `E2E Test Product ${ts}`;
  const recipeName = `E2E Test Recipe ${ts}`;

  return {
    products: [
      {
        name: productName,
        caloriesPer100g: 100,
        proteinPer100g: 10,
        carbsPer100g: 20,
        fatPer100g: 5,
        unit: 'g',
      },
    ],
    recipes: [
      {
        name: recipeName,
        description: 'Test recipe',
        servings: 2,
        ingredients: [
          {
            product: productName,
            amount: 200,
            unit: 'g',
          },
        ],
      },
    ],
    schedule: [
      {
        date: new Date().toISOString().slice(0, 10),
        meals: [
          {
            type: 'lunch',
            recipe: recipeName,
            servings: 1,
          },
        ],
      },
    ],
  };
}

export function createSampleProduct() {
  return {
    name: `Test Product ${Date.now()}`,
    caloriesPer100g: 150,
    proteinPer100g: 15,
    carbsPer100g: 10,
    fatPer100g: 8,
  };
}

export function createSampleRecipe() {
  return {
    name: `Test Recipe ${Date.now()}`,
    description: 'A test recipe',
    servings: 4,
    prepTimeMinutes: 30,
  };
}
