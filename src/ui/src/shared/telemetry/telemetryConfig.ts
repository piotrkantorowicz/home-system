export interface TelemetryConfig {
  enabled: boolean;
  /** Origin of the API: the only one that receives `traceparent`. */
  apiOrigin: string;
  serviceVersion: string;
}

interface TelemetryInput {
  /** `window.__CONFIG__.tracingEnabled` (deploy-time, public/config.js). */
  runtimeEnabled: boolean | undefined;
  /** `VITE_TRACING_ENABLED` (build-time fallback). */
  envEnabled: string | undefined;
  /** `VITE_API_BASE_URL`; empty or undefined means the API is same-origin. */
  apiBaseUrl: string | undefined;
  dev: boolean;
  locationOrigin: string;
  version: string;
}

/** Off unless the runtime config (wins) or the build variable turns it on. */
export function resolveTelemetryConfig(input: TelemetryInput): TelemetryConfig {
  const base = input.apiBaseUrl ?? (input.dev ? 'http://localhost:5050' : '');
  return {
    enabled: input.runtimeEnabled ?? input.envEnabled === 'true',
    apiOrigin: base ? new URL(base, input.locationOrigin).origin : input.locationOrigin,
    serviceVersion: input.version,
  };
}

/** Matches URLs under the API origin and nothing else (never Authentik or other third parties). */
export function propagationUrls(apiOrigin: string): RegExp[] {
  return [new RegExp(`^${apiOrigin.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}(/|$)`)];
}
