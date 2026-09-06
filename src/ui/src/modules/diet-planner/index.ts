import { NAV_GROUP_SETTINGS } from '@shared/lib/module-registry';
import {
  Home,
  Package,
  BookOpen,
  CalendarDays,
  Upload,
  ShoppingCart,
  BarChart2,
  Droplets,
  Target,
  SlidersHorizontal,
} from 'lucide-react';
import { lazy } from 'react';

import en from './locales/en.json';
import pl from './locales/pl.json';

import type { AppModule } from '@shared/lib/module-registry';

const Dashboard = lazy(() => import('./pages/Dashboard'));
const ProductList = lazy(() => import('./pages/products/ProductList'));
const ProductCreate = lazy(() => import('./pages/products/ProductCreate'));
const ProductEdit = lazy(() => import('./pages/products/ProductEdit'));
const ProductDetail = lazy(() => import('./pages/products/ProductDetail'));
const RecipeList = lazy(() => import('./pages/recipes/RecipeList'));
const RecipeCreate = lazy(() => import('./pages/recipes/RecipeCreate'));
const RecipeEdit = lazy(() => import('./pages/recipes/RecipeEdit'));
const RecipeDetail = lazy(() => import('./pages/recipes/RecipeDetail'));
const Calendar = lazy(() => import('./pages/Calendar'));
const ShoppingList = lazy(() => import('./pages/ShoppingList'));
const ImportWizard = lazy(() => import('./pages/diet-plans/ImportWizard'));
const NutritionSummary = lazy(() => import('./pages/NutritionSummary'));
const Profile = lazy(() => import('./pages/Profile'));
const Hydration = lazy(() => import('./pages/Hydration'));
const Preferences = lazy(() => import('./pages/Preferences'));
const ControlKit = lazy(() => import('./pages/ControlKit'));

export const dietPlannerModule: AppModule = {
  name: 'diet-planner',
  translationKey: 'common.diet_planner',
  description: 'Track your diet, plan meals, and monitor nutrition',
  basePath: '/diet-planner',
  icon: CalendarDays,
  localeNamespaces: ['diet-planner'],
  i18nResources: {
    en: { 'diet-planner': en },
    pl: { 'diet-planner': pl },
  },
  navItems: [
    // Plan
    {
      name: 'Dashboard',
      href: '/diet-planner',
      icon: Home,
      translationKey: 'common.dashboard',
      group: 'nav_groups.plan',
    },
    {
      name: 'Meal plan',
      href: '/diet-planner/calendar',
      icon: CalendarDays,
      translationKey: 'common.meal_plan',
      group: 'nav_groups.plan',
    },
    {
      name: 'Import plan',
      href: '/diet-planner/import',
      icon: Upload,
      translationKey: 'common.import_plan',
      group: 'nav_groups.plan',
    },
    // Library
    {
      name: 'Products',
      href: '/diet-planner/products',
      icon: Package,
      translationKey: 'common.products',
      group: 'nav_groups.library',
    },
    {
      name: 'Recipes',
      href: '/diet-planner/recipes',
      icon: BookOpen,
      translationKey: 'common.recipes',
      group: 'nav_groups.library',
    },
    {
      name: 'Shopping list',
      href: '/diet-planner/shopping-list',
      icon: ShoppingCart,
      translationKey: 'common.shopping_list',
      group: 'nav_groups.library',
    },
    // Track
    {
      name: 'Nutrition',
      href: '/diet-planner/nutrition',
      icon: BarChart2,
      translationKey: 'common.nutrition',
      group: 'nav_groups.track',
    },
    {
      name: 'Hydration',
      href: '/diet-planner/hydration',
      icon: Droplets,
      translationKey: 'common.hydration',
      group: 'nav_groups.track',
    },
    {
      name: 'Goals',
      href: '/diet-planner/profile?section=goals',
      icon: Target,
      translationKey: 'common.goals',
      group: 'nav_groups.track',
    },
    // Pinned at the bottom, below a divider
    {
      name: 'Preferences',
      href: '/diet-planner/preferences',
      icon: SlidersHorizontal,
      translationKey: 'common.preferences',
      group: NAV_GROUP_SETTINGS,
    },
  ],
  routes: [
    { index: true, Component: Dashboard },
    { path: 'products', Component: ProductList },
    { path: 'products/new', Component: ProductCreate },
    { path: 'products/:id', Component: ProductDetail },
    { path: 'products/:id/edit', Component: ProductEdit },
    { path: 'recipes', Component: RecipeList },
    { path: 'recipes/new', Component: RecipeCreate },
    { path: 'recipes/:id', Component: RecipeDetail },
    { path: 'recipes/:id/edit', Component: RecipeEdit },
    { path: 'calendar', Component: Calendar },
    { path: 'shopping-list', Component: ShoppingList },
    { path: 'import', Component: ImportWizard },
    { path: 'nutrition', Component: NutritionSummary },
    { path: 'profile', Component: Profile },
    { path: 'hydration', Component: Hydration },
    { path: 'preferences', Component: Preferences },
    { path: 'control-kit', Component: ControlKit },
  ],
};
