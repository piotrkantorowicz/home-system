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

  async expectLoggedOut() {
    await expect(this.page).toHaveURL(/localhost:9000\/if\/flow\//);
    await expect(this.page.getByRole('heading', { name: /you've logged out of/i })).toBeVisible();
  }
}
