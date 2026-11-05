import { routePattern } from './routePattern';

describe('routePattern', () => {
  it('joins relative segments into the pattern, not the concrete path', () => {
    const matches = [
      { route: {} },
      { route: { path: 'diet-planner' } },
      { route: { path: 'recipes/:id' } },
    ];
    expect(routePattern(matches)).toBe('/diet-planner/recipes/:id');
  });

  it('lets an absolute child path replace its parents', () => {
    expect(routePattern([{ route: { path: 'x' } }, { route: { path: '/settings/app' } }])).toBe(
      '/settings/app',
    );
  });

  it('returns / for the root and keeps splat routes', () => {
    expect(routePattern([{ route: { path: '/' } }])).toBe('/');
    expect(routePattern([{ route: {} }])).toBe('/');
    expect(routePattern([{ route: { path: '*' } }])).toBe('/*');
  });
});
