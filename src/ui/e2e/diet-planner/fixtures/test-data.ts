export const sampleImportData = {
  products: [
    {
      name: `E2E Test Product ${Date.now()}`,
      caloriesPer100g: 100,
      proteinPer100g: 10,
      carbsPer100g: 20,
      fatPer100g: 5,
      unit: 'g',
    },
  ],
  recipes: [
    {
      name: `E2E Test Recipe ${Date.now()}`,
      description: 'Test recipe',
      servings: 2,
      ingredients: [
        {
          product: `E2E Test Product ${Date.now()}`,
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
          recipe: `E2E Test Recipe ${Date.now()}`,
          servings: 1,
        },
      ],
    },
  ],
};

export const sampleProduct = {
  name: `Test Product ${Date.now()}`,
  caloriesPer100g: 150,
  proteinPer100g: 15,
  carbsPer100g: 10,
  fatPer100g: 8,
};

export const sampleRecipe = {
  name: `Test Recipe ${Date.now()}`,
  description: 'A test recipe',
  servings: 4,
  prepTimeMinutes: 30,
};
