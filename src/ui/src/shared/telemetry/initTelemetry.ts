import type { TelemetryConfig } from './telemetryConfig';
import type { DataRouter } from 'react-router-dom';

/**
 * Loads the OpenTelemetry web SDK after first render. With tracing off the chunk is never
 * requested. Telemetry must never break the app, so a failed load is only logged.
 */
export async function initTelemetry(router: DataRouter, config: TelemetryConfig): Promise<void> {
  if (!config.enabled) return;
  try {
    const { startTelemetry } = await import('./otel');
    startTelemetry(router, config);
  } catch (error) {
    console.warn('Tracing failed to start', error);
  }
}
