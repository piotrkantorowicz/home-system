interface MatchLike {
  route: { path?: string };
}

/** `/diet-planner/recipes/:id` from the matched route chain: the pattern, never the concrete path. */
export function routePattern(matches: readonly MatchLike[]): string {
  let pattern = '';
  for (const { route } of matches) {
    const path = route.path;
    if (!path) continue;
    pattern = path.startsWith('/') ? path : `${pattern}/${path}`;
  }
  return pattern.replace(/\/{2,}/g, '/').replace(/(.)\/$/, '$1') || '/';
}
