# ESLint + Prettier Setup

## `eslint.config.ts`

```ts
import js from "@eslint/js";
import ts from "typescript-eslint";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import jsxA11y from "eslint-plugin-jsx-a11y";
import importPlugin from "eslint-plugin-import";
import { globalIgnores } from "eslint/config";

export default ts.config(
  globalIgnores(["dist", "coverage", ".storybook"]),

  // Base JS rules
  js.configs.recommended,

  // TypeScript
  ...ts.configs.strictTypeChecked,
  ...ts.configs.stylisticTypeChecked,
  {
    languageOptions: {
      parserOptions: {
        project: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
  },

  // React
  {
    plugins: {
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
      "jsx-a11y": jsxA11y,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      ...jsxA11y.configs.recommended.rules,
      "react-refresh/only-export-components": ["warn", { allowConstantExport: true }],
    },
  },

  // Imports
  {
    plugins: { import: importPlugin },
    rules: {
      "import/no-default-export": "error",         // enforce named exports
      "import/no-cycle": "error",                   // catch circular deps
      "import/no-duplicates": "error",
      "import/order": [
        "warn",
        {
          groups: ["builtin", "external", "internal", "parent", "sibling", "index", "type"],
          "newlines-between": "always",
          alphabetize: { order: "asc" },
        },
      ],
    },
  },

  // Custom project rules
  {
    rules: {
      // TypeScript strictness
      "@typescript-eslint/no-explicit-any": "error",
      "@typescript-eslint/consistent-type-imports": ["error", { prefer: "type-imports" }],
      "@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_" }],
      "@typescript-eslint/no-non-null-assertion": "error",
      "@typescript-eslint/prefer-nullish-coalescing": "error",
      "@typescript-eslint/prefer-optional-chain": "error",
      "@typescript-eslint/no-floating-promises": "error",
      "@typescript-eslint/await-thenable": "error",

      // React
      "react-hooks/exhaustive-deps": "error",      // warn is too quiet — error

      // General
      "no-console": ["warn", { allow: ["warn", "error"] }],
      "prefer-const": "error",
      "no-var": "error",
      eqeqeq: ["error", "always"],
    },
  },

  // Relax default-export rule for route/page files only
  {
    files: ["src/pages/**/*.tsx", "src/app/App.tsx"],
    rules: { "import/no-default-export": "off" },
  },
);
```

---

## `.prettierrc`

```json
{
  "semi": true,
  "singleQuote": false,
  "trailingComma": "all",
  "printWidth": 100,
  "tabWidth": 2,
  "useTabs": false,
  "bracketSameLine": false,
  "arrowParens": "always",
  "endOfLine": "lf",
  "plugins": ["prettier-plugin-tailwindcss"]
}
```

## `.prettierignore`

```
dist
coverage
*.min.js
public
```

---

## Package.json scripts

```json
{
  "scripts": {
    "dev":          "vite",
    "build":        "tsc -b && vite build",
    "preview":      "vite preview",
    "lint":         "eslint .",
    "lint:fix":     "eslint . --fix",
    "format":       "prettier --write .",
    "format:check": "prettier --check .",
    "typecheck":    "tsc --noEmit",
    "test":         "vitest",
    "test:ui":      "vitest --ui",
    "test:coverage":"vitest run --coverage",
    "test:e2e":     "playwright test",
    "check":        "npm run typecheck && npm run lint && npm run test:coverage"
  }
}
```

---

## Pre-commit hooks (lint-staged + husky)

```json
// package.json
{
  "lint-staged": {
    "*.{ts,tsx}": ["eslint --fix", "prettier --write"],
    "*.{json,css,md}": ["prettier --write"]
  }
}
```

```sh
# .husky/pre-commit
npx lint-staged
```
