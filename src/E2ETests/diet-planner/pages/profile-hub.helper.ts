import type { Page } from '@playwright/test';

export type ProfileSection =
  | 'body-stats'
  | 'goals'
  | 'meal-schedule'
  | 'hydration'
  | 'notifications';

export async function gotoProfileSection(page: Page, section: ProfileSection): Promise<void> {
  await page.goto(`/diet-planner/profile?section=${section}`);
  await page.waitForLoadState('networkidle');
}
