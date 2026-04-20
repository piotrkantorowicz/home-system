import { Home, Package, BookOpen, CalendarDays } from 'lucide-react';
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
const ImportWizard = lazy(() => import('./pages/diet-plans/ImportWizard'));
const Goals = lazy(() => import('./pages/Goals'));
const NutritionSummary = lazy(() => import('./pages/NutritionSummary'));
const MealSchedule = lazy(() => import('./pages/MealSchedule'));
const Profile = lazy(() => import('./pages/Profile'));
const NotificationPreferences = lazy(() => import('./pages/NotificationPreferences'));
const Hydration = lazy(() => import('./pages/Hydration'));

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
    {
      name: 'Dashboard',
      href: '/diet-planner',
      icon: Home,
      translationKey: 'common.dashboard',
    },
    {
      name: 'Products',
      href: '/diet-planner/products',
      icon: Package,
      translationKey: 'common.products',
    },
    {
      name: 'Recipes',
      href: '/diet-planner/recipes',
      icon: BookOpen,
      translationKey: 'common.recipes',
    },
    {
      name: 'Calendar',
      href: '/diet-planner/calendar',
      icon: CalendarDays,
      translationKey: 'common.calendar',
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
    { path: 'goals', Component: Goals },
    { path: 'calendar', Component: Calendar },
    { path: 'import', Component: ImportWizard },
    { path: 'nutrition', Component: NutritionSummary },
    { path: 'meal-schedule', Component: MealSchedule },
    { path: 'profile', Component: Profile },
    { path: 'notification-preferences', Component: NotificationPreferences },
    { path: 'hydration', Component: Hydration },
  ],
};
