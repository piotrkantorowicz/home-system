import { initTelemetry } from './initTelemetry';

import type { DataRouter } from 'react-router-dom';

const startTelemetry = vi.fn();
const otelLoaded = vi.fn();
vi.mock('./otel', () => {
  otelLoaded();
  return { startTelemetry };
});

const router = {} as DataRouter;
const config = { apiOrigin: 'http://api', serviceVersion: '1.0.0' };

describe('initTelemetry', () => {
  beforeEach(() => vi.clearAllMocks());

  it('never loads the OTel chunk when tracing is off', async () => {
    await initTelemetry(router, { ...config, enabled: false });
    expect(otelLoaded).not.toHaveBeenCalled();
    expect(startTelemetry).not.toHaveBeenCalled();
  });

  it('loads the chunk and starts tracing when enabled', async () => {
    const enabled = { ...config, enabled: true };
    await initTelemetry(router, enabled);
    expect(startTelemetry).toHaveBeenCalledWith(router, enabled);
  });
});
