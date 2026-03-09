import { format, addDays, startOfWeek } from 'date-fns';

export const generateWeeklyPlan = (planName: string, startFromDate: Date) => {
  const startDate = startOfWeek(startFromDate, { weekStartsOn: 1 }); // Monday
  const endDate = addDays(startDate, 6);

  const formatDate = (d: Date) => format(d, 'yyyy-MM-dd');

  // Core Products
  const products = [
    {
      name: `Super Oats ${Date.now()}`,
      caloriesPer100g: 100,
      proteinPer100g: 10,
      carbsPer100g: 60,
      fatPer100g: 5,
      unit: 'g',
    },
    {
      name: `Power Eggs ${Date.now()}`,
      caloriesPer100g: 150,
      proteinPer100g: 13,
      carbsPer100g: 1,
      fatPer100g: 11,
      unit: 'piece',
      gramPerPiece: 50,
    },
    {
      name: `Lean Chicken ${Date.now()}`,
      caloriesPer100g: 165,
      proteinPer100g: 31,
      carbsPer100g: 0,
      fatPer100g: 3.6,
      unit: 'g',
    },
    {
      name: `Broccoli ${Date.now()}`,
      caloriesPer100g: 34,
      proteinPer100g: 2.8,
      carbsPer100g: 7,
      fatPer100g: 0.4,
      unit: 'g',
    },
    {
      name: `Brown Rice ${Date.now()}`,
      caloriesPer100g: 111,
      proteinPer100g: 2.6,
      carbsPer100g: 23,
      fatPer100g: 0.9,
      unit: 'g',
    },
  ];

  // Core Recipes
  const recipes = [
    {
      name: `Morning Bowl ${Date.now()}`,
      description: 'Healthy start',
      servings: 1,
      prepTimeMinutes: 5,
      ingredients: [
        { product: products[0].name, amount: 50, unit: 'g' }, // 50kcal
        { product: products[1].name, amount: 2, unit: 'piece' }, // ~150kcal (if 100g total? No, 2 pieces * 50g = 100g -> 150kcal)
      ],
      instructions: 'Boil oats, fry eggs.',
    },
    {
      name: `Chicken Rice ${Date.now()}`,
      description: 'Lunch staple',
      servings: 2,
      ingredients: [
        { product: products[2].name, amount: 300, unit: 'g' },
        { product: products[4].name, amount: 200, unit: 'g' },
        { product: products[3].name, amount: 150, unit: 'g' },
      ],
    },
  ];

  const schedule = [];
  for (let i = 0; i < 7; i++) {
    const currentDay = addDays(startDate, i);
    const dateStr = formatDate(currentDay);

    // Mon, Wed, Fri get Morning Bowl
    if (i % 2 === 0) {
      schedule.push({
        date: dateStr,
        meals: [{ type: 'breakfast', recipe: recipes[0].name, servings: 1 }],
      });
    } else {
      // Tue, Thu, Sat, Sun get Chicken Rice
      schedule.push({
        date: dateStr,
        meals: [{ type: 'lunch', recipe: recipes[1].name, servings: 1 }],
      });
    }
  }

  return {
    planName,
    startDate: formatDate(startDate),
    endDate: formatDate(endDate),
    products,
    recipes,
    schedule,
    // Helpers for test assertions
    productNames: products.map((p) => p.name),
    recipeNames: recipes.map((r) => r.name),
    targetProduct: products[0].name, // The oats
    targetRecipe: recipes[0].name, // The bowl
  };
};
