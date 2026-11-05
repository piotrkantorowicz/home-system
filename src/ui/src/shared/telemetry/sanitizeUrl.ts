/** Drops query and fragment: the OIDC callback carries `code`/`state` there. */
export function stripQueryAndFragment(url: string): string {
  return url.replace(/[?#].*$/, '');
}
