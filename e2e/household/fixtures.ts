// Re-export the diet-planner auth fixture so household specs share the same
// per-worker authenticated session setup. When/if multiple modules need this,
// lift the fixture into e2e/shared/.
export { test, expect } from '../diet-planner/fixtures';
