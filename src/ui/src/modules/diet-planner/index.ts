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
const DietPlanList = lazy(() => import('./pages/diet-plans/DietPlanList'));
const DietPlanDetail = lazy(() => import('./pages/diet-plans/DietPlanDetail'));
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
      name: 'Diet Plans',
      href: '/diet-planner/diet-plans',
      icon: CalendarDays,
      translationKey: 'common.diet_plans',
    },
    {
      name: 'Goals',
      href: '/diet-planner/goals',
      icon: Target,
      translationKey: 'diet-planner:goals.nav',
    },
    {
      name: 'Import Plan',
      href: '/diet-planner/diet-plans/import',
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
    { path: 'diet-plans', Component: DietPlanList },
    { path: 'diet-plans/import', Component: ImportWizard },
    { path: 'diet-plans/:id', Component: DietPlanDetail },
  ],
};
