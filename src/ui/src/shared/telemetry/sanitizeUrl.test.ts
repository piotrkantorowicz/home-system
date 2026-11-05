import { stripQueryAndFragment } from './sanitizeUrl';

describe('stripQueryAndFragment', () => {
  it('drops the OIDC callback code and state', () => {
    expect(stripQueryAndFragment('https://app/callback?code=abc&state=xyz#f')).toBe(
      'https://app/callback',
    );
  });

  it('leaves a clean URL alone', () => {
    expect(stripQueryAndFragment('https://app/a/b')).toBe('https://app/a/b');
  });
});
