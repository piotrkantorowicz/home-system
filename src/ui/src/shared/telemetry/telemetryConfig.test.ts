import { propagationUrls, resolveTelemetryConfig } from './telemetryConfig';

const base = {
  runtimeEnabled: undefined,
  envEnabled: undefined,
  apiBaseUrl: undefined,
  dev: false,
  locationOrigin: 'https://app.example.com',
  version: '1.2.3',
};

describe('resolveTelemetryConfig', () => {
  it('is off by default', () => {
    expect(resolveTelemetryConfig(base).enabled).toBe(false);
  });

  it('is enabled by the build variable and overridden by the runtime config', () => {
    expect(resolveTelemetryConfig({ ...base, envEnabled: 'true' }).enabled).toBe(true);
    expect(
      resolveTelemetryConfig({ ...base, envEnabled: 'true', runtimeEnabled: false }).enabled,
    ).toBe(false);
    expect(resolveTelemetryConfig({ ...base, runtimeEnabled: true }).enabled).toBe(true);
  });

  it('uses the page origin when the API is same-origin', () => {
    expect(resolveTelemetryConfig(base).apiOrigin).toBe('https://app.example.com');
  });

  it('uses the API base URL origin and the dev default', () => {
    expect(
      resolveTelemetryConfig({ ...base, apiBaseUrl: 'https://api.example.com/v1' }).apiOrigin,
    ).toBe('https://api.example.com');
    expect(resolveTelemetryConfig({ ...base, dev: true }).apiOrigin).toBe('http://localhost:5050');
  });
});

describe('propagationUrls', () => {
  const urls = propagationUrls('https://api.example.com');
  const matches = (url: string) => urls.some((re) => re.test(url));

  it('matches the API origin only', () => {
    expect(matches('https://api.example.com/api/v1/products?x=1')).toBe(true);
    expect(matches('https://api.example.com')).toBe(true);
  });

  it('excludes Authentik and look-alike hosts', () => {
    expect(matches('https://auth.example.com/application/o/token/')).toBe(false);
    expect(matches('https://api.example.com.evil.io/x')).toBe(false);
    expect(matches('http://localhost:9000/application/o/token/')).toBe(false);
  });
});
