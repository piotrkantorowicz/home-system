import { http, HttpResponse } from 'msw';

const BASE = 'http://localhost:5000';

export const productHandlers = [
  http.get(`${BASE}/api/v1/products`, () => {
    return HttpResponse.json({
      items: [
        {
          id: '11111111-1111-1111-1111-111111111111',
          name: 'Chicken Breast',
          caloriesPer100g: 165,
          proteinPer100g: 31,
          carbsPer100g: 0,
          fatPer100g: 3.6,
          fiberPer100g: null,
          defaultUnit: 'g',
          densityGramsPerMl: null,
          gramPerPiece: null,
          isOwner: true,
          createdAt: '2024-01-01T00:00:00Z',
        },
      ],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
    });
  }),

  http.get(`${BASE}/api/v1/products/:id`, ({ params }) => {
    if (params.id === '11111111-1111-1111-1111-111111111111') {
      return HttpResponse.json({
        id: '11111111-1111-1111-1111-111111111111',
        name: 'Chicken Breast',
        caloriesPer100g: 165,
        proteinPer100g: 31,
        carbsPer100g: 0,
        fatPer100g: 3.6,
        fiberPer100g: null,
        defaultUnit: 'g',
        densityGramsPerMl: null,
        gramPerPiece: null,
        isOwner: true,
        createdAt: '2024-01-01T00:00:00Z',
      });
    }
    return HttpResponse.json({ title: 'Not found' }, { status: 404 });
  }),

  http.post(`${BASE}/api/v1/products`, () => {
    return HttpResponse.json('22222222-2222-2222-2222-222222222222', { status: 201 });
  }),

  http.put(`${BASE}/api/v1/products/:id`, () => {
    return new HttpResponse(null, { status: 204 });
  }),

  http.delete(`${BASE}/api/v1/products/:id`, () => {
    return new HttpResponse(null, { status: 204 });
  }),
];

export const importHandlers = [
  http.post(`${BASE}/api/v1/meals/validate`, () => {
    return HttpResponse.json({
      valid: true,
      canProceed: true,
      summary: { errors: 0, warnings: 0, info: 0 },
      issues: [],
      plan: {
        productsToCreate: 2,
        productsToReuse: 0,
        recipesToCreate: 1,
        recipesToReuse: 0,
        mealEntriesToCreate: 2,
      },
    });
  }),

  http.post(`${BASE}/api/v1/meals/import`, () => {
    return HttpResponse.json({ importedCount: 2 }, { status: 200 });
  }),
];

export const mealHandlers = [
  http.get(`${BASE}/api/v1/meals`, () => {
    return HttpResponse.json([
      {
        id: '33333333-3333-3333-3333-333333333333',
        userId: 'user-1',
        date: '2024-01-15',
        mealType: 'breakfast',
        recipeId: '44444444-4444-4444-4444-444444444444',
        recipeName: 'Oatmeal',
        servings: 1,
        notes: null,
        mealTime: null,
        sequenceOrder: null,
        createdAt: '2024-01-15T07:00:00Z',
      },
    ]);
  }),

  http.get(`${BASE}/api/v1/meals/nutrition-summary`, () => {
    return HttpResponse.json([
      {
        date: '2024-01-15',
        calories: 450,
        protein: 20,
        carbs: 60,
        fat: 10,
        fiber: 5,
      },
    ]);
  }),
];

export const goalHandlers = [
  http.get(`${BASE}/api/v1/goals`, () => {
    return HttpResponse.json({
      id: '55555555-5555-5555-5555-555555555555',
      userId: 'user-1',
      dailyCalorieTarget: 2000,
      proteinGrams: 150,
      carbsGrams: 250,
      fatGrams: 70,
      fiberGrams: 30,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: null,
    });
  }),

  http.put(`${BASE}/api/v1/goals`, () => {
    return HttpResponse.json({
      id: '55555555-5555-5555-5555-555555555555',
      userId: 'user-1',
      dailyCalorieTarget: 2200,
      proteinGrams: 150,
      carbsGrams: 250,
      fatGrams: 70,
      fiberGrams: 30,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    });
  }),

  http.post(`${BASE}/api/v1/goals`, () => {
    return HttpResponse.json('55555555-5555-5555-5555-555555555555', { status: 201 });
  }),
];

export const mealScheduleHandlers = [
  http.get(`${BASE}/api/v1/meal-schedule`, () => {
    return HttpResponse.json({
      id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      userId: 'user-1',
      slots: [
        {
          id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          name: 'Breakfast',
          defaultTime: '07:00',
          sortOrder: 0,
        },
        {
          id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
          name: 'Lunch',
          defaultTime: '12:00',
          sortOrder: 1,
        },
        {
          id: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
          name: 'Dinner',
          defaultTime: '18:00',
          sortOrder: 2,
        },
      ],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: null,
    });
  }),

  http.put(`${BASE}/api/v1/meal-schedule`, () => {
    return new HttpResponse(null, { status: 204 });
  }),
];

export const profileHandlers = [
  http.get(`${BASE}/api/v1/profile`, () => {
    return HttpResponse.json({
      id: '66666666-6666-6666-6666-666666666666',
      userId: 'user-1',
      dateOfBirth: '1990-05-15',
      gender: 'Male',
      heightCm: 180,
      currentWeightKg: 80,
      targetWeightKg: 75,
      activityLevel: 'ModeratelyActive',
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: null,
    });
  }),

  http.post(`${BASE}/api/v1/profile`, () => {
    return HttpResponse.json('66666666-6666-6666-6666-666666666666', { status: 201 });
  }),

  http.put(`${BASE}/api/v1/profile`, () => {
    return new HttpResponse(null, { status: 204 });
  }),
];

export const notificationPreferencesHandlers = [
  http.get(`${BASE}/api/v1/notification-preferences`, () => {
    return HttpResponse.json({
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      userId: 'user-1',
      mealReminderEnabled: true,
      mealReminderLeadTimeMinutes: 15,
      waterReminderEnabled: true,
      waterReminderIntervalMinutes: 60,
      weeklySummaryEnabled: true,
      goalMilestoneAlertsEnabled: true,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: null,
    });
  }),

  http.put(`${BASE}/api/v1/notification-preferences`, () => {
    return new HttpResponse(null, { status: 204 });
  }),
];

export const hydrationHandlers = [
  http.get(`${BASE}/api/v1/hydration/config`, () => {
    return HttpResponse.json({
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      userId: 'user-1',
      dailyWaterTargetMl: 2500,
      glassSizeMl: 250,
      trackWaterIntake: true,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    });
  }),

  http.put(`${BASE}/api/v1/hydration/config`, () => {
    return new HttpResponse(null, { status: 204 });
  }),

  http.get(`${BASE}/api/v1/hydration/intake`, () => {
    return HttpResponse.json({
      date: '2024-01-15',
      totalMl: 500,
      entries: [
        {
          id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          amountMl: 250,
          timestamp: '2024-01-15T08:00:00Z',
          note: null,
        },
        {
          id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          amountMl: 250,
          timestamp: '2024-01-15T10:00:00Z',
          note: 'Morning',
        },
      ],
    });
  }),

  http.post(`${BASE}/api/v1/hydration/intake`, () => {
    return HttpResponse.json('dddddddd-dddd-dddd-dddd-dddddddddddd', { status: 201 });
  }),

  http.delete(`${BASE}/api/v1/hydration/intake/:id`, () => {
    return new HttpResponse(null, { status: 204 });
  }),
];

export const recipeHandlers = [
  http.get(`${BASE}/api/v1/recipes/:id`, ({ params }) => {
    if (params.id === '11111111-1111-1111-1111-111111111111') {
      return HttpResponse.json({
        id: '11111111-1111-1111-1111-111111111111',
        name: 'Test Recipe',
        description: 'A test recipe',
        instructions: 'Mix everything',
        servings: 2,
        prepTimeMinutes: 15,
        ingredients: [
          {
            productId: '11111111-1111-1111-1111-111111111111',
            productName: 'Chicken Breast',
            amount: 200,
            unit: 'g',
          },
        ],
        createdAt: '2024-01-01T00:00:00Z',
      });
    }
    return HttpResponse.json({ title: 'Not found' }, { status: 404 });
  }),

  http.post(`${BASE}/api/v1/recipes`, () => {
    return HttpResponse.json('33333333-3333-3333-3333-333333333333', { status: 201 });
  }),

  http.put(`${BASE}/api/v1/recipes/:id`, () => {
    return new HttpResponse(null, { status: 204 });
  }),
];

export const handlers = [
  ...productHandlers,
  ...importHandlers,
  ...mealHandlers,
  ...goalHandlers,
  ...mealScheduleHandlers,
  ...profileHandlers,
  ...notificationPreferencesHandlers,
  ...hydrationHandlers,
  ...recipeHandlers,
];
