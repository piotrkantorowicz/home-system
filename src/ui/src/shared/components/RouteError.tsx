import { Button } from '@shared/components/ui';
import { useTranslation } from 'react-i18next';
import {
  isRouteErrorResponse,
  Link,
  useLocation,
  useNavigate,
  useRouteError,
} from 'react-router-dom';

const AUTH_PATHS = ['/callback', '/silent-renew'];

/**
 * Route-level error element: replaces React Router's developer error page.
 * Retry reloads the current route; on the auth callbacks the one-time code is spent, so
 * retry restarts from the start page instead.
 */
export function RouteError() {
  const { t } = useTranslation();
  const error = useRouteError();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  // No error at all means this was rendered by a catch-all `*` route.
  const notFound =
    error === undefined || error === null || (isRouteErrorResponse(error) && error.status === 404);

  function retry() {
    if (AUTH_PATHS.includes(pathname)) void navigate('/', { replace: true });
    else void navigate(0);
  }

  return (
    <div role="alert" className="flex min-h-[60vh] items-center justify-center p-8">
      <div className="max-w-md text-center">
        <h1 className="text-26px mb-2 font-bold tracking-tight">
          {t(notFound ? 'common.route_error.not_found_title' : 'common.route_error.title')}
        </h1>
        <p className="text-muted-foreground mb-6">
          {t(notFound ? 'common.route_error.not_found_message' : 'common.route_error.message')}
        </p>
        {import.meta.env.DEV && error instanceof Error && (
          <pre className="bg-muted text-muted-foreground mb-6 max-h-48 overflow-auto rounded-lg border p-3 text-left text-xs">
            {error.message}
          </pre>
        )}
        <div className="flex flex-wrap justify-center gap-2">
          {!notFound && <Button onClick={retry}>{t('common.route_error.retry')}</Button>}
          <Button asChild variant={notFound ? 'default' : 'outline'}>
            <Link to="/">{t('common.route_error.home')}</Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
