import { W3CTraceContextPropagator } from '@opentelemetry/core';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { resourceFromAttributes } from '@opentelemetry/resources';
import {
  BatchSpanProcessor,
  StackContextManager,
  WebTracerProvider,
} from '@opentelemetry/sdk-trace-web';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';
import { onCLS, onINP, onLCP } from 'web-vitals';

import { routePattern } from './routePattern';
import { stripQueryAndFragment } from './sanitizeUrl';
import { propagationUrls } from './telemetryConfig';

import type { TelemetryConfig } from './telemetryConfig';
import type { Span, SpanProcessor } from '@opentelemetry/sdk-trace-web';
import type { DataRouter } from 'react-router-dom';

const URL_ATTRIBUTES = ['url.full', 'http.url'];

// Same-origin ingress route (home-lab); the Vite dev server proxies it when configured.
const OTLP_PATH = '/otlp/v1/traces';

const urlSanitizer: SpanProcessor = {
  onStart: () => undefined,
  onEnd: () => undefined,
  onEnding(span: Span) {
    for (const key of URL_ATTRIBUTES) {
      const value = span.attributes[key];
      if (typeof value === 'string') span.setAttribute(key, stripQueryAndFragment(value));
    }
  },
  forceFlush: () => Promise.resolve(),
  shutdown: () => Promise.resolve(),
};

export function startTelemetry(router: DataRouter, config: TelemetryConfig): void {
  const provider = new WebTracerProvider({
    resource: resourceFromAttributes({
      [ATTR_SERVICE_NAME]: 'home-system-ui',
      [ATTR_SERVICE_VERSION]: config.serviceVersion,
    }),
    spanProcessors: [
      urlSanitizer,
      new BatchSpanProcessor(new OTLPTraceExporter({ url: OTLP_PATH })),
    ],
  });
  provider.register({
    contextManager: new StackContextManager(),
    propagator: new W3CTraceContextPropagator(),
  });

  registerInstrumentations({
    tracerProvider: provider,
    instrumentations: [
      new DocumentLoadInstrumentation(),
      new FetchInstrumentation({
        propagateTraceHeaderCorsUrls: propagationUrls(config.apiOrigin),
        ignoreUrls: [OTLP_PATH],
      }),
    ],
  });

  const tracer = provider.getTracer('home-system-ui');

  // One span per completed route change, named by the pattern (`/diet-planner/recipes/:id`).
  let lastLocation: unknown;
  const recordRoute = (state: DataRouter['state']) => {
    if (state.navigation.state !== 'idle' || state.location === lastLocation) return;
    lastLocation = state.location;
    const pattern = routePattern(state.matches);
    tracer.startSpan(`route ${pattern}`, { attributes: { 'http.route': pattern } }).end();
  };
  recordRoute(router.state);
  router.subscribe(recordRoute);

  // web-vitals may call back again on later visibility changes; report each metric once per load.
  const reported = new Set<string>();
  const report = (metric: { name: string; value: number; rating: string }) => {
    if (reported.has(metric.name)) return;
    reported.add(metric.name);
    tracer
      .startSpan(`web-vital ${metric.name}`, {
        attributes: {
          'web_vital.name': metric.name,
          'web_vital.value': metric.value,
          'web_vital.rating': metric.rating,
        },
      })
      .end();
  };
  onLCP(report);
  onINP(report);
  onCLS(report);
}
