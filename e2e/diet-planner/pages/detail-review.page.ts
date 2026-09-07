import { expect, type Page } from "@playwright/test";

export class DetailReviewPage {
  constructor(private readonly page: Page) {}

  async openProduct(name: string) {
    await this.page.getByRole("link", { name, exact: true }).click();
    await expect(
      this.page.getByRole("heading", { name: "Nutrition Facts (per 100g)" }),
    ).toBeVisible();
    await expect(this.page.getByText("Protein", { exact: true })).toHaveCount(
      1,
    );
    await expect(
      this.page.getByText("Macro Distribution", { exact: true }),
    ).toHaveCount(0);
  }

  async scaleRecipe() {
    await expect(this.page.getByText("100.0 g", { exact: true })).toBeVisible();
    await this.page.getByRole("button", { name: "Increase servings" }).click();
    await expect(this.page.getByText("150.0 g", { exact: true })).toBeVisible();
    await expect(this.page.getByText(/^50\s*kcal$/)).toBeVisible();
    await expect(
      this.page.getByText("Total for 3 servings: 150 kcal"),
    ).toBeVisible();
  }

  async planRecipe(name: string) {
    await this.page
      .getByRole("button", { name: "Add to plan", exact: true })
      .click();
    const dialog = this.page.getByRole("dialog");
    await expect(dialog.getByLabel("Recipe", { exact: true })).toHaveValue(
      name,
    );
    await expect(dialog.getByLabel("Servings", { exact: true })).toHaveValue(
      "3",
    );
    await dialog
      .getByLabel("Meal Type", { exact: true })
      .selectOption({ label: "Breakfast" });
    await dialog.getByRole("button", { name: "Add Meal", exact: true }).click();
    await expect(this.page).toHaveURL(/calendar\?view=day&date=/);
    await expect(
      this.page.getByRole("heading", { name: "Calendar", exact: true }),
    ).toBeVisible();
    await expect(this.page.getByText(name, { exact: true })).toBeVisible();
  }

  async jumpToToday() {
    await this.page.goto("/diet-planner/calendar?view=week&date=2020-01-01");
    await this.page.getByRole("button", { name: "Today", exact: true }).click();
    await expect(this.page).toHaveURL(/calendar\?view=day$/);
    const today = new Date().toLocaleDateString("en", {
      weekday: "long",
      month: "long",
      day: "numeric",
    });
    const heading = this.page.getByRole("heading", {
      name: today,
      exact: true,
    });
    await expect(heading).toBeVisible();
    await expect(heading).toBeFocused();
    await expect(
      this.page.getByRole("button", { name: today, exact: true }),
    ).toHaveAttribute("aria-pressed", "true");
    await this.page.goBack();
    await expect(this.page).toHaveURL(/view=week&date=2020-01-01/);
  }

  async captureResponsive(outputPath: (name: string) => string, name: string) {
    for (const dismiss of await this.page
      .getByRole("button", { name: "Dismiss notification" })
      .all()) {
      await dismiss.click();
    }
    await expect(
      this.page.getByRole("button", { name: "Dismiss notification" }),
    ).toHaveCount(0);
    await this.page.emulateMedia({ reducedMotion: "reduce" });
    for (const width of [390, 1280]) {
      await this.page.setViewportSize({ width, height: 1000 });
      for (const theme of ["light", "dark"]) {
        await this.page.evaluate((value) => {
          document.documentElement.classList.remove("light", "dark");
          document.documentElement.classList.add(value);
        }, theme);
        await expect
          .poll(() =>
            this.page
              .getByRole("main")
              .evaluate((el) => el.scrollWidth <= el.clientWidth),
          )
          .toBe(true);
        await this.page.screenshot({
          path: outputPath(`${name}-${width}-${theme}.png`),
          fullPage: true,
          animations: "disabled",
        });
      }
    }
  }
}
