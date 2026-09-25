import { BasePage } from '../../diet-planner/pages/BasePage';

import type { Locator, Page } from '@playwright/test';

export class HouseholdPage extends BasePage {
  readonly heading: Locator;
  readonly addMemberButton: Locator;
  readonly myInvitationsHeading: Locator;
  readonly leaveButton: Locator;
  readonly confirmButton: Locator;

  constructor(page: Page) {
    super(page);
    this.heading = page.getByRole('heading', { level: 1 });
    this.addMemberButton = page.getByRole('button', { name: /add member/i });
    this.myInvitationsHeading = page.getByRole('heading', {
      name: /invitations for you/i,
    });
    this.leaveButton = page.getByRole('button', { name: /leave household/i });
    this.confirmButton = page.getByRole('button', { name: /^confirm$/i });
  }

  async goto() {
    await this.page.goto('/household');
    await this.heading.waitFor();
  }

  async inviteByEmail(email: string, role: 'Owner' | 'Adult' | 'Child' | 'Guest' = 'Adult') {
    await this.addMemberButton.click();
    await this.page.getByRole('tab', { name: /by email/i }).click();
    await this.page.getByLabel(/email address/i).fill(email);
    await this.page.getByLabel(/^role$/i).selectOption(role);
    await this.page.getByRole('button', { name: /invite by email/i }).click();
    // The dialog closes on success — a real state change, unlike the transient toast.
    await this.page.getByRole('dialog').waitFor({ state: 'hidden' });
  }

  /** The pending-invitation row shown on the invitee's own "Invitations for you" panel. */
  invitationFrom(householdName: string): Locator {
    return this.page.getByRole('listitem').filter({ hasText: householdName });
  }

  async acceptInvitationFrom(householdName: string) {
    await this.myInvitationsHeading.waitFor();
    await this.invitationFrom(householdName)
      .getByRole('button', { name: /^accept$/i })
      .click();
    // Wait for the real effect (membership now shows in place of the invitations
    // panel) rather than the transient join toast, which is prone to disappearing
    // before a slower poll notices it.
    await this.page.getByRole('heading', { level: 1, name: householdName }).waitFor();
  }

  async declineInvitationFrom(householdName: string) {
    await this.myInvitationsHeading.waitFor();
    const row = this.invitationFrom(householdName);
    await row.getByRole('button', { name: /^decline$/i }).click();
    // The row disappears on the real effect, rather than waiting on the toast.
    await row.waitFor({ state: 'hidden' });
  }

  async leaveHousehold() {
    await this.leaveButton.click();
    await this.confirmButton.click();
  }
}
