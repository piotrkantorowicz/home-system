import { lazy } from 'react';
import { Home, Package, BookOpen, CalendarDays, Upload, Target } from 'lucide-react';
import type { AppModule } from '@shared/lib/module-registry';
import en from './locales/en.json';
import pl from './locales/pl.json';

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

export const dietPlannerModule: AppModule = {
  name: 'diet-planner',
  translationKey: 'common.diet_planner',
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
    {
      name: 'Goals',
      href: '/diet-planner/goals',
      icon: Target,
      translationKey: 'common.goals',
    },
    {
      name: 'Import Plan',
      href: '/diet-planner/import',
      icon: Upload,
      translationKey: 'common.import_plan',
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
  ],
};
