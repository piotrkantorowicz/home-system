import { expect, type Page } from '@playwright/test';

export class LogoutPage {
  constructor(private readonly page: Page) {}

  async goto() {
    await this.page.goto('/');
    await expect(this.page.getByRole('button', { name: /user menu/i })).toBeVisible();
  }

  async logout() {
    await this.page.getByRole('button', { name: /user menu/i }).click();
    await this.page.getByRole('menuitem', { name: /logout/i }).click();
  }

  /** App logout leaves Authentik's own session up; switching accounts ends that one too. */
  async logoutOfAuthentik() {
    await this.page.getByRole('link', { name: /log out of authentik/i }).click();
    await expect(this.page.getByRole('link', { name: /log out of authentik/i })).toHaveCount(0);
  }

  async expectLoggedOut() {
    await expect(this.page).toHaveURL(/localhost:9000\/if\/flow\//);
    await expect(this.page.getByRole('heading', { name: /you've logged out of/i })).toBeVisible();
  }
}
