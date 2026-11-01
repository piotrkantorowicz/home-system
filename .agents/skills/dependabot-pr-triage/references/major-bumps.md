# Major bumps seen in this repo

## msw 2 -> 3 (PR #772)
- `onUnhandledRequest` was renamed `onUnhandledFrame`. The old key fails type-check.
- Fix lives in `src/test/setup.ts`.
- Verify: type-check, lint, build, full Vitest run.

## SkiaSharp 3 -> 4 (PR #775)
- The native assets package must match the managed package version. Bump both together.
- `DrawBitmap` without `SKSamplingOptions` is obsolete. Under warnings-as-errors it breaks the build.
- Pass explicit sampling options. Nearest sampling keeps output identical when draws sit at integer offsets.
- Verify: build, unit tests, image integration tests.
