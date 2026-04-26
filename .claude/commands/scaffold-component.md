---
description: Create a new shared UI component with variants, tests, and proper exports
---

Create a new shared UI component with variants, tests, and proper exports.

## Usage

```
/scaffold-component <ComponentName> [--with-variants]
```

Examples:
```
/scaffold-component Badge
/scaffold-component Card --with-variants
```

## Instructions

Read `.claude/rules/frontend-react-typescript.md` and `.claude/rules/frontend-styling.md` before generating.

### Generate:

**`src/components/ui/{ComponentName}.tsx`**

```tsx
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/utils/cn";

const {camelName}Variants = cva(
  // base classes
  "...",
  {
    variants: {
      variant: {
        default: "...",
        // add variants as needed
      },
      size: {
        sm: "...",
        md: "...",
        lg: "...",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "md",
    },
  }
);

export interface {ComponentName}Props
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof {camelName}Variants> {
  // add component-specific props here
}

/** {ComponentName} — a shared UI primitive. */
export function {ComponentName}({
  className,
  variant,
  size,
  children,
  ...props
}: {ComponentName}Props) {
  return (
    <div className={cn({camelName}Variants({ variant, size }), className)} {...props}>
      {children}
    </div>
  );
}
```

If `--with-variants` is NOT specified, skip `cva` and generate a simpler component without variant props.

**`src/components/ui/{ComponentName}.test.tsx`**

```tsx
import { render, screen } from "@testing-library/react";
import { {ComponentName} } from "./{ComponentName}";

describe("{ComponentName}", () => {
  it("renders children", () => {
    render(<{ComponentName}>Hello</{ComponentName}>);
    expect(screen.getByText("Hello")).toBeInTheDocument();
  });

  it("applies custom className", () => {
    const { container } = render(
      <{ComponentName} className="custom-class">Content</{ComponentName}>
    );
    expect(container.firstChild).toHaveClass("custom-class");
  });

  // If variants exist:
  it("renders with variant prop", () => {
    render(<{ComponentName} variant="default">Content</{ComponentName}>);
    expect(screen.getByText("Content")).toBeInTheDocument();
  });
});
```

### After generating:

- The component uses **named export** (never default)
- The component uses `cn()` for class merging
- Accessibility: uses semantic HTML element appropriate for the component type
- Props interface is exported
- JSDoc summary is present on the component function
- Remind user to re-export from `src/components/ui/index.ts` if a barrel file exists
