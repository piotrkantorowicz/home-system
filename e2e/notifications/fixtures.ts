// Re-export the diet-planner auth fixture so notifications specs share the
// same authenticated session setup. When/if multiple modules need this, lift
// the fixture into e2e/shared/.
export { test, expect } from '../diet-planner/fixtures';
