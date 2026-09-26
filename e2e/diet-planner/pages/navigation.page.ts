import { BasePage } from './BasePage';

export class NavigationPage extends BasePage {
  async goto() {
    await this.page.goto('/diet-planner');
    await this.page.getByRole('heading', { name: 'Today', level: 1 }).waitFor();
  }

  get sectionNav() {
    return this.page.getByRole('navigation', { name: 'Diet Planner' });
  }

  get mobileNav() {
    return this.page.getByRole('navigation', { name: 'Diet Planner' });
  }

  get moduleRail() {
    return this.page.getByRole('navigation', { name: 'Modules' });
  }

  get palette() {
    return this.page.getByRole('dialog', { name: 'Search everything' });
  }

  get collapseButton() {
    return this.page.getByRole('button', { name: 'Collapse' });
  }

  get expandButton() {
    return this.page.getByRole('button', { name: 'Expand' });
  }

  get mobileModuleSwitcher() {
    return this.page.getByRole('button', { name: 'Switch module' });
  }

  get householdMenuItem() {
    return this.page.getByRole('menu', { name: 'Switch module' }).getByRole('menuitem').first();
  }

  get notificationsModule() {
    return this.moduleRail.getByRole('button', { name: 'Notifications' });
  }

  get productsSectionLink() {
    return this.sectionNav.getByRole('link', { name: 'Products' });
  }

  get productsMobileLink() {
    return this.mobileNav.getByRole('link', { name: 'Products' });
  }

  get paletteSearch() {
    return this.palette.getByRole('textbox', { name: 'Search everything' });
  }

  get productsPaletteOption() {
    return this.palette.getByRole('option', { name: /Products.*Diet Planner/ });
  }

  get householdMobileNav() {
    return this.page.getByRole('navigation', { name: 'Household' });
  }

  get moduleSwitcherTrigger() {
    return this.moduleRail.getByRole('button', { name: 'Switch module' });
  }

  get switcherMenu() {
    return this.page.getByRole('menu', { name: 'Switch module' });
  }

  /** Matches both languages — the trigger's accessible name is translated. */
  get userMenuButton() {
    return this.page.getByRole('button', {
      name: /user menu|menu użytkownika/i,
    });
  }

  get languageSwitcherButton() {
    return this.page.getByRole('button', { name: /^(EN|PL)$/ });
  }

  get preferencesLink() {
    return this.page.getByRole('menuitem', {
      name: /^(Preferences|Preferencje)$/,
    });
  }

  /** Not scoped to `sectionNav` — its accessible name is itself translated. */
  sectionLink(name: string | RegExp) {
    return this.page.getByRole('link', { name });
  }

  /**
   * `AppShell` persists the last-visited module to localStorage in a passive
   * effect, one render tick after the URL changes — `toHaveURL` alone can
   * race ahead of it under load. Root-redirect assertions must wait for this
   * first.
   */
  async waitForLastModule(moduleName: string) {
    await this.page.waitForFunction(
      ({ key, value }) => window.localStorage.getItem(key) === value,
      { key: 'home-system-last-module', value: moduleName },
    );
  }
}
