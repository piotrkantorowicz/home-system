# Styling — Tailwind CSS v4

## Setup (Tailwind v4 / Vite)

```ts
// vite.config.ts
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
});
```

```css
/* src/app/globals.css */
@import "tailwindcss";

@theme {
  /* Design tokens — single source of truth */
  --color-primary:      oklch(55% 0.22 260);
  --color-primary-fg:   oklch(98% 0.01 260);
  --color-surface:      oklch(98% 0.005 260);
  --color-surface-alt:  oklch(94% 0.01 260);
  --color-border:       oklch(85% 0.01 260);
  --color-text:         oklch(20% 0.01 260);
  --color-text-muted:   oklch(50% 0.01 260);
  --color-error:        oklch(55% 0.22 30);
  --color-success:      oklch(55% 0.18 150);
  --color-warning:      oklch(70% 0.18 80);

  --font-sans:   "Geist", ui-sans-serif, system-ui;
  --font-mono:   "Geist Mono", ui-monospace;

  --radius-sm:   0.25rem;
  --radius-md:   0.5rem;
  --radius-lg:   0.75rem;
  --radius-xl:   1rem;

  --shadow-sm:   0 1px 2px oklch(0% 0 0 / 0.05);
  --shadow-md:   0 4px 6px oklch(0% 0 0 / 0.07), 0 1px 3px oklch(0% 0 0 / 0.06);
  --shadow-lg:   0 10px 15px oklch(0% 0 0 / 0.1), 0 4px 6px oklch(0% 0 0 / 0.05);
}
```

---

## Class composition with `cva` (class-variance-authority)

Always use `cva` for variant-driven components. Never build variant logic with string interpolation.

```tsx
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/utils/cn";

const buttonVariants = cva(
  // base classes
  "inline-flex items-center justify-center gap-2 rounded-md font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary disabled:pointer-events-none disabled:opacity-50",
  {
    variants: {
      variant: {
        primary:   "bg-primary text-primary-fg hover:bg-primary/90",
        secondary: "bg-surface-alt text-text border border-border hover:bg-surface",
        ghost:     "hover:bg-surface-alt text-text",
        danger:    "bg-error text-white hover:bg-error/90",
      },
      size: {
        sm:  "h-8 px-3 text-sm",
        md:  "h-10 px-4 text-sm",
        lg:  "h-12 px-6 text-base",
        icon:"size-10",
      },
    },
    defaultVariants: {
      variant: "primary",
      size: "md",
    },
  }
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {}

export function Button({ className, variant, size, ...props }: ButtonProps) {
  return (
    <button className={cn(buttonVariants({ variant, size }), className)} {...props} />
  );
}
```

```ts
// utils/cn.ts
import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
```

---

## Rules

### Do

- Use `cn()` for all conditional/merged class strings.
- Use `cva` for any component with more than one visual variant.
- Use design tokens (`--color-*`, `--radius-*`) defined in `@theme` — never hardcode hex/rgb values.
- Group Tailwind classes: layout → box model → typography → colors → effects → states.
- Use `@layer components` sparingly — prefer utility classes.
- Dark mode via `dark:` variant and a `data-theme` attribute on `<html>`.

### Don't

- No inline `style={{ }}` for anything expressible as a utility class.
- No Tailwind `arbitrary values` (`[color:#abc]`) for anything that should be a design token.
- No `!important` utilities (`!text-red-500`) — fix specificity instead.
- No CSS modules mixed with Tailwind on the same element.
- No string interpolation for class names — Tailwind can't statically analyse them:

```tsx
// ❌ Tailwind cannot detect this at build time
const color = isError ? "red" : "green";
<p className={`text-${color}-500`}>…</p>

// ✅ Full class names only
const className = isError ? "text-error" : "text-success";
<p className={className}>…</p>
```

---

## Responsive design

Use mobile-first breakpoints. Order: base → `sm:` → `md:` → `lg:` → `xl:`.

```tsx
<div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
```

---

## Animations

Prefer CSS transitions for simple state changes. Use `tailwindcss-animate` or custom keyframes for complex ones.

```css
/* globals.css */
@keyframes fade-in {
  from { opacity: 0; transform: translateY(4px); }
  to   { opacity: 1; transform: translateY(0); }
}

@theme {
  --animate-fade-in: fade-in 200ms ease-out both;
}
```

```tsx
<div className="animate-fade-in">…</div>
```

For orchestrated animations, use **Motion** (`motion/react`):

```tsx
import { motion } from "motion/react";

<motion.div
  initial={{ opacity: 0, y: 8 }}
  animate={{ opacity: 1, y: 0 }}
  transition={{ duration: 0.2 }}
>
```

---

## Dark mode

```css
/* globals.css */
@media (prefers-color-scheme: dark) {
  :root {
    --color-surface:   oklch(15% 0.005 260);
    --color-text:      oklch(95% 0.005 260);
    /* … override all tokens … */
  }
}
/* Or via class for user-toggle: */
[data-theme="dark"] { … }
```
