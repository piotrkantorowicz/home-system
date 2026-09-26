import type { Locator, Page } from '@playwright/test';

export type ProfileSection =
  'body-stats' | 'goals' | 'meal-schedule' | 'hydration' | 'notifications';

/**
 * Every profile section renders a spinner until its query resolves and only
 * then mounts the form, so the section's submit button doubles as the
 * "data is loaded" signal.
 */
export function profileSectionReady(page: Page, section: ProfileSection): Locator {
  const saveButtonName: Record<ProfileSection, RegExp> = {
    'body-stats': /save profile/i,
    goals: /save goals/i,
    'meal-schedule': /save schedule/i,
    hydration: /save settings/i,
    notifications: /^save$/i,
  };
  return page.getByRole('button', { name: saveButtonName[section] });
}

export async function gotoProfileSection(page: Page, section: ProfileSection): Promise<void> {
  await page.goto(`/diet-planner/profile?section=${section}`);
  await profileSectionReady(page, section).waitFor();
}
