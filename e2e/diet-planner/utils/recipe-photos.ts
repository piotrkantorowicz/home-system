import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

import { createApiContext } from './seed';

import type { Page } from '@playwright/test';

export type Visibility = 'Private' | 'Household' | 'Public';

export interface PhotoPayload {
  name: string;
  mimeType: string;
  buffer: Buffer;
}

/** Declared as PNG, but not an image — the server must reject it after the type check. */
export const MALFORMED_PHOTO: PhotoPayload = {
  name: 'broken.png',
  mimeType: 'image/png',
  buffer: Buffer.from('this is not a png'),
};

/** One byte over the 10 MiB upload limit. */
export const OVERSIZED_PHOTO: PhotoPayload = {
  name: 'huge.jpg',
  mimeType: 'image/jpeg',
  buffer: Buffer.alloc(10 * 1024 * 1024 + 1),
};

export interface SeededRecipe {
  id: string;
  name: string;
  productId: string;
}

/** Deterministic 64×48 test photos; A and B differ so a replacement is observable. */
export const PHOTO_A = fileURLToPath(
  new URL('../fixtures/images/recipe-photo-a.png', import.meta.url),
);
export const PHOTO_B = fileURLToPath(
  new URL('../fixtures/images/recipe-photo-b.jpg', import.meta.url),
);

/** Creates a one-ingredient recipe through the API; the UI create flow is covered elsewhere. */
export async function seedPhotoRecipe(
  page: Page,
  name: string,
  visibility: Visibility,
): Promise<SeededRecipe> {
  const api = await createApiContext(page);
  try {
    const product = await api.post('/api/v1/products', {
      data: {
        name: `${name} base`,
        calories: 120,
        protein: 4,
        carbs: 20,
        fat: 2,
        fiber: null,
        defaultUnit: 'g',
        densityGramsPerMl: null,
        gramPerPiece: null,
        visibility,
      },
    });
    if (!product.ok())
      throw new Error(`seedPhotoRecipe: POST product returned ${product.status()}`);
    const productId = (await product.json()) as string;

    const recipe = await api.post('/api/v1/recipes', {
      data: recipeBody(name, productId, visibility),
    });
    if (!recipe.ok()) throw new Error(`seedPhotoRecipe: POST recipe returned ${recipe.status()}`);
    return { id: (await recipe.json()) as string, name, productId };
  } finally {
    await api.dispose();
  }
}

export async function setRecipeVisibility(
  page: Page,
  recipe: SeededRecipe,
  visibility: Visibility,
): Promise<void> {
  const api = await createApiContext(page);
  try {
    const res = await api.put(`/api/v1/recipes/${recipe.id}`, {
      data: recipeBody(recipe.name, recipe.productId, visibility),
    });
    if (!res.ok()) throw new Error(`setRecipeVisibility: PUT returned ${res.status()}`);
  } finally {
    await api.dispose();
  }
}

/** PUTs a photo as multipart and returns the HTTP status; arrange-only, the UI upload has its own test. */
export async function putPhoto(
  page: Page,
  recipeId: string,
  file: string | PhotoPayload = PHOTO_A,
): Promise<number> {
  const api = await createApiContext(page);
  try {
    const res = await api.put(`/api/v1/recipes/${recipeId}/image`, {
      multipart: {
        file:
          typeof file === 'string'
            ? {
                name: path.basename(file),
                mimeType: file.endsWith('.png') ? 'image/png' : 'image/jpeg',
                buffer: fs.readFileSync(file),
              }
            : file,
      },
    });
    return res.status();
  } finally {
    await api.dispose();
  }
}

/** Fetches the photo the way a copied endpoint URL would be used — with the caller's own token. */
export async function photoStatus(
  page: Page,
  recipeId: string,
  variant: 'detail' | 'thumbnail' = 'detail',
): Promise<number> {
  const api = await createApiContext(page);
  try {
    const suffix = variant === 'thumbnail' ? '/thumbnail' : '';
    return (await api.get(`/api/v1/recipes/${recipeId}/image${suffix}`)).status();
  } finally {
    await api.dispose();
  }
}

/** The recipe's current image revision and served detail bytes, to prove a photo did not change. */
export async function photoSnapshot(
  page: Page,
  recipeId: string,
): Promise<{ version: string | undefined; bytes: Buffer }> {
  const api = await createApiContext(page);
  try {
    const recipe = (await (await api.get(`/api/v1/recipes/${recipeId}`)).json()) as {
      image?: { version: string } | null;
    };
    const image = await api.get(`/api/v1/recipes/${recipeId}/image`);
    if (!image.ok()) throw new Error(`photoSnapshot: GET image returned ${image.status()}`);
    return { version: recipe.image?.version, bytes: await image.body() };
  } finally {
    await api.dispose();
  }
}

export async function removePhotoStatus(page: Page, recipeId: string): Promise<number> {
  const api = await createApiContext(page);
  try {
    return (await api.delete(`/api/v1/recipes/${recipeId}/image`)).status();
  } finally {
    await api.dispose();
  }
}

/** Deletes the recipe; the API clears its photo reference and queues object cleanup. */
export async function deleteRecipe(page: Page, recipeId: string): Promise<void> {
  const api = await createApiContext(page);
  try {
    const res = await api.delete(`/api/v1/recipes/${recipeId}`);
    if (!res.ok() && res.status() !== 404) {
      throw new Error(`deleteRecipe: DELETE returned ${res.status()}`);
    }
  } finally {
    await api.dispose();
  }
}

function recipeBody(name: string, productId: string, visibility: Visibility) {
  return {
    name,
    description: null,
    instructions: null,
    servings: 1,
    prepTimeMinutes: null,
    ingredients: [{ productId, amount: 100, unit: 'g' }],
    visibility,
  };
}
