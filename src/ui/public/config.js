// Deploy-time settings read by src/shared/auth/authConfig.ts. Empty here, so the VITE_* build
// variables (or the dev defaults) apply; the cluster mounts its own config.js over this file.
window.__CONFIG__ = {};
// Browser tracing is off by default; set { tracingEnabled: true } to load the OTel web SDK.
