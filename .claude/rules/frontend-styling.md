# Styling — Tailwind CSS v4

## Setup that exists

Tailwind v4 via `@tailwindcss/postcss` (`postcss.config.js`). **No `tailwind.config.js`** —
v4 is CSS-first; the theme lives in `src/ui/src/index.css` under `@theme`. Classes are sorted
by `prettier-plugin-tailwindcss`.

```css
/* src/index.css */
@import 'tailwindcss';

@theme {
  --color-background: hsl(260 20% 97%);
  --color-foreground: hsl(264 12% 15%);
  --color-card: …;  --color-popover: …;
  --color-primary: hsl(261 75% 54%);   --color-primary-foreground: …;
  --color-secondary / --color-muted / --color-accent (+ -foreground)
  --color-destructive, --color-border, --color-input, --color-ring
  --color-success, --color-warning
  /* redesign additions */
  --color-text-2, --color-border-strong, --color-primary-soft, --color-primary-ink
  --color-protein, --color-carbs, --color-fat, --color-fiber, --color-water, --color-good
  --radius-xl: 1.375rem;  --radius-lg: 0.8125rem;  --radius-md: 0.6875rem;  --radius-sm: 0.5rem;
}
[data-theme='dark'] { /* same tokens, dark values */ }
```

- Tokens are declared as full `hsl(...)` values. Consume them as `bg-primary`,
  `text-muted-foreground`, or in custom CSS as `var(--color-primary)` — **never**
  `hsl(var(--color-primary))` (double-wrapping breaks the colour).
- Dark mode is the `data-theme` attribute on `<html>` (`ThemeContext`), plus a
  `prefers-color-scheme` fallback for the `system` preference. Use the `dark:` variant only
  when a token cannot express the difference.

## Component variants — `cva`

```tsx
const buttonVariants = cva(
  'inline-flex items-center justify-center rounded-lg font-medium transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: { default: 'bg-primary text-primary-foreground …', outline: '…', ghost: '…', destructive: '…' },
      size: { default: 'h-11 px-5', sm: 'h-9 px-3.5 text-sm', icon: 'size-10' },
    },
    defaultVariants: { variant: 'default', size: 'default' },
  },
);

export interface ButtonProps
  extends React.ComponentProps<'button'>, VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}
```

- Any component with more than one look uses `cva` + `cn()` (`@shared/lib/utils`, clsx + tailwind-merge).
- Callers pass `className` for layout (margins, grid placement) — never to restyle the
  component's own look. If a caller needs a new look, add a variant.

## Rules

**Do**
- Tokens for every colour, radius and shadow. `bg-card`, `rounded-lg`, `text-text-2`.
- Group classes logically; the Prettier plugin fixes the order.
- Mobile-first breakpoints: base → `sm:` → `md:` → `lg:` → `xl:`. The shell switches rail →
  bottom tab bar below `md`.
- `size-*` for square boxes, `gap-*` over margins between siblings, logical properties
  (`ps-`, `pe-`, `ms-`) where RTL could matter.

**Don't**
- `style={{}}` for anything a utility expresses. Inline style is acceptable only for values
  computed at runtime (chart geometry, progress widths, CSS variables from data).
- `!important` utilities (`!text-…`). Fix specificity or the variant.
- String-built class names (`text-${color}-500`) — Tailwind cannot see them. Map to full
  class strings.
- New arbitrary values (`rounded-[13px]`, `text-[13.5px]`). The redesign introduced ~300 of
  them; promoting that scale into `@theme` (`--text-*`, `--radius-*`, `--spacing-*`) is
  tracked in #269. Until then: reuse an existing arbitrary value from a
  neighbouring component rather than inventing a new one, and never add one where a token
  already fits.
- CSS modules or styled-components. Global CSS is `index.css` only.

## Animation

- Transitions on state changes via utilities (`transition-all duration-200 ease-out`,
  `active:scale-[0.97]` is the sanctioned press feedback in `Button`).
- Keyframes go into `index.css` and are exposed through `@theme { --animate-*: … }`, used as
  `animate-fade-in`.
- No animation library. Radix handles enter/exit for dialogs, sheets, popovers via
  `data-[state=open]:` variants.
- Respect `motion-reduce:` — any non-trivial animation gets a `motion-reduce:transition-none`.

## Accessibility of styles

- Never remove `focus-visible:ring-*` from primitives.
- Colour is never the only signal — pair macro colours (`protein`, `carbs`, `fat`) with a label
  or icon.
- Minimum tap target 44 px on touch layouts (`h-11` default button, `size-10` icon minimum).
