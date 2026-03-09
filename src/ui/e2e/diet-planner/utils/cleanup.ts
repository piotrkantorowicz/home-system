import { request, APIRequestContext } from '@playwright/test';
import fs from 'fs';
import path from 'path';
import { getTrackedIds, clearTracker } from './test-tracker';

/**
 * Permanently delete a single item by ID.
 * Uses ?permanent=true to bypass soft-delete.
 */
async function permanentDeleteById(
  apiContext: APIRequestContext,
  endpoint: string,
  id: string,
  label: string
) {
  const res = await apiContext.delete(`${endpoint}/${id}?permanent=true`);
  if (res.ok()) {
    console.log(`  Permanently deleted ${label}: ${id}`);
  } else if (res.status() === 404) {
    console.log(`  ${label} ${id} already gone (404)`);
  } else {
    console.warn(`  Failed to permanently delete ${label} ${id}: ${res.status()}`);
  }
}

/**
 * Query all visible items from a paginated endpoint and permanently delete each one.
 */
async function permanentDeleteAll(
  apiContext: APIRequestContext,
  endpoint: string,
  label: string,
  queryParams = 'onlyMine=true'
) {
  let page = 1;

  while (true) {
    const res = await apiContext.get(`${endpoint}?${queryParams}&pageSize=100&page=${page}`);
    if (!res.ok()) break;
    const data = await res.json();
    const items = data.items || [];
    if (items.length === 0) break;

    for (const item of items) {
      await permanentDeleteById(apiContext, endpoint, item.id, label);
    }

    if (items.length < 100) break;
    page++;
  }
}

export async function cleanupTestData() {
  const authStatePath = path.resolve('playwright/.auth/user.json');

  if (!fs.existsSync(authStatePath)) {
    console.warn('No auth state found at ' + authStatePath + ', skipping cleanup');
    return;
  }

  try {
    const authState = JSON.parse(fs.readFileSync(authStatePath, 'utf-8'));
    const origin = authState.origins.find((o: any) => o.origin.includes('localhost'));
    const storageItem = origin?.localStorage.find((i: any) => i.name.startsWith('oidc.user:'));

    if (!storageItem) {
      console.warn('No OIDC user found in storage, skipping cleanup');
      return;
    }

    const user = JSON.parse(storageItem.value);
    const token = user.access_token;

    const apiContext = await request.newContext({
      baseURL: 'http://localhost:5000',
      extraHTTPHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });

    console.log('Starting permanent cleanup of test data...');

    // --- Phase 1: Permanently delete tracked IDs (includes soft-deleted items invisible to queries) ---
    const tracked = getTrackedIds();
    const trackedTotal = tracked.plans.length + tracked.recipes.length + tracked.products.length;
    console.log(
      `Found ${trackedTotal} tracked items (${tracked.plans.length} plans, ${tracked.recipes.length} recipes, ${tracked.products.length} products)`
    );

    // Deletion order respects FK constraints:
    // 1. Plans (permanent delete cascades to meal_entries)
    // 2. Recipes (safe now — no meal_entries reference them)
    // 3. Products (safe now — no recipe_ingredients reference them)
    //
    // We interleave tracked IDs and visible-item queries to handle
    // import side-effects (recipes/products created by import but not tracked).

    // Step 1: Delete all plans (tracked + visible)
    if (tracked.plans.length > 0) {
      console.log('Permanently deleting tracked plans...');
      for (const id of tracked.plans) {
        await permanentDeleteById(apiContext, '/api/v1/diet-plans', id, 'plan');
      }
    }
    await permanentDeleteAll(apiContext, '/api/v1/diet-plans', 'plan', 'pageSize=100');

    // Step 2: Delete all recipes (tracked + visible)
    if (tracked.recipes.length > 0) {
      console.log('Permanently deleting tracked recipes...');
      for (const id of tracked.recipes) {
        await permanentDeleteById(apiContext, '/api/v1/recipes', id, 'recipe');
      }
    }
    await permanentDeleteAll(apiContext, '/api/v1/recipes', 'recipe');

    // Step 3: Delete all products (tracked + visible)
    if (tracked.products.length > 0) {
      console.log('Permanently deleting tracked products...');
      for (const id of tracked.products) {
        await permanentDeleteById(apiContext, '/api/v1/products', id, 'product');
      }
    }
    await permanentDeleteAll(apiContext, '/api/v1/products', 'product');

    console.log('Permanent cleanup complete.');
    clearTracker();
    await apiContext.dispose();
  } catch (error) {
    console.error('Error during cleanup:', error);
  }
}
