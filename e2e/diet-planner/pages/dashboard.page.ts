import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

/**
 * The dashboard was rebuilt for the #208 redesign: it no longer shows
 * product/recipe/calendar quick-stat cards (that entry point moved into the
 * two-tier nav's grouped section panel). It now shows one "Today"
 * summary surface (calories, macros and water), a "Meals" list with check
 * toggles, and a "Last 7 days" review card.
 */
export class DashboardPage extends BasePage {
  readonly heading: Locator;
  readonly logWaterLink: Locator;
  readonly logMealButton: Locator;
  readonly fullPlanLink: Locator;
  readonly waterProgress: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { name: 'Today', level: 1 });
    this.logWaterLink = page.getByRole('link', { name: /log water/i });
    // "Log a meal" appears twice — the header's primary action and, with the
    // same label, the Next up card's empty-state CTA. Both open the same
    // MealForm sheet; take the header's (first in DOM order).
    this.logMealButton = page.getByRole('button', { name: /log a meal/i }).first();
    this.fullPlanLink = page.getByRole('link', { name: /open meal plan/i });
    this.waterProgress = page.getByRole('progressbar', { name: 'Water' });
  }

  async goto() {
    await this.page.goto('/diet-planner');
    await this.heading.waitFor();
  }

  /** The Water summary writes progress as "0.8 of 2.5 L" (litres, one decimal). */
  waterTotal(amountMl: number, goalMl: number) {
    const litres = (ml: number) => (Math.round(ml / 100) / 10).toFixed(1);
    return this.page
      .locator('p')
      .filter({ has: this.page.locator('strong', { hasText: litres(amountMl) }) })
      .filter({ hasText: `of ${litres(goalMl)} L` });
  }
}
