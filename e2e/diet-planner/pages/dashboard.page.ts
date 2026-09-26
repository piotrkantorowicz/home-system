import { BasePage } from './BasePage';

import type { Page, Locator } from '@playwright/test';

/**
 * The dashboard was rebuilt for the #208 redesign: it no longer shows
 * product/recipe/calendar quick-stat cards (that entry point moved into the
 * two-tier nav's grouped section panel). It now shows a "Today" hero
 * (calories left/eaten/target + macro bars), a "Next up" meal card, a water
 * card, and a "This week" review card.
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
    this.fullPlanLink = page.getByRole('link', { name: /full plan/i });
    this.waterProgress = page.getByRole('progressbar', { name: 'Water' });
  }

  async goto() {
    await this.page.goto('/diet-planner');
    await this.heading.waitFor();
  }

  waterTotal(amountMl: number) {
    return this.page.getByText(String(amountMl), { exact: true });
  }
}
