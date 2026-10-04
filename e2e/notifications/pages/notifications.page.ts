import { expect } from '../fixtures';

import type { Page } from '@playwright/test';

export class NotificationsPage {
  private readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async preferences() {
    await this.page.goto('/diet-planner/settings/app');
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
    // The unread counter lives on the sidebar's Notifications link (the header bell is mobile-only).
    const badge = this.page
      .getByRole('complementary')
      .getByRole('link', { name: /notifications/i })
      .getByLabel(/\d+ unread/);
    if (total === 0) await expect(badge).toHaveCount(0);
    else await expect(badge).toHaveAttribute('aria-label', `${total} unread`);
  }

  unreadRows(slotName: string) {
    return this.rows(slotName).filter({ has: this.page.getByRole('img', { name: 'Unread' }) });
  }

  // Opening an item marks it read and navigates to its destination, so return to the inbox after.
  async readFirst(slotName: string) {
    await this.unreadRows(slotName).first().getByRole('button').click();
    await this.inbox();
  }

  async readRemaining(slotName: string) {
    while ((await this.unreadRows(slotName).count()) > 0) {
      await this.unreadRows(slotName).first().getByRole('button').click();
      await this.inbox();
    }
  }
}
