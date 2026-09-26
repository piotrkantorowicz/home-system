import { expect } from '../fixtures';

import type { Page } from '@playwright/test';

export class NotificationsPage {
  private readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async preferences() {
    await this.page.goto('/notifications/preferences');
    await expect(this.realTimeSwitch()).toBeEnabled();
  }

  realTimeSwitch() {
    return this.page.getByRole('switch', { name: /real-time/i });
  }

  async inbox() {
    await this.page.goto('/notifications');
    await expect(this.page.getByRole('heading', { name: 'Notifications', level: 1 })).toBeVisible();
  }

  rows(slotName: string) {
    return this.page.getByRole('listitem').filter({ hasText: `Missed ${slotName}` });
  }

  async expectUnreadCount(total: number) {
    const badge = this.page
      .getByRole('button', { name: /open notifications/i })
      .getByLabel(/\d+ unread/);
    if (total === 0) await expect(badge).toHaveCount(0);
    else await expect(badge).toHaveAttribute('aria-label', `${total} unread`);
  }

  async readFirst(slotName: string) {
    await this.rows(slotName).first().getByRole('button').click();
  }

  async readRemaining(slotName: string) {
    const checkboxes = this.rows(slotName).getByRole('checkbox');
    for (let index = 0; index < (await checkboxes.count()); index++) {
      if (await checkboxes.nth(index).isEnabled()) await checkboxes.nth(index).check();
    }
    await this.page.getByRole('button', { name: 'Mark as read (2)', exact: true }).click();
  }
}
