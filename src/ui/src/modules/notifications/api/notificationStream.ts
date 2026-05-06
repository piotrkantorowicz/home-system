import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr';
import { getValidToken } from '@shared/api/tokenInterceptor';

const baseUrl: string =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  (import.meta.env.DEV ? 'http://localhost:5050' : '');

export interface NotificationPayload {
  deliveryId: string;
  notificationId: string;
  type: string;
  title: string;
  body: string;
  createdAt: string;
}

let connection: HubConnection | null = null;
let startPromise: Promise<HubConnection> | null = null;

function build(): HubConnection {
  const c = new HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/notifications`, {
      accessTokenFactory: async () => (await getValidToken()) ?? '',
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
    .build();

  c.serverTimeoutInMilliseconds = 60_000;
  c.keepAliveIntervalInMilliseconds = 10_000;

  return c;
}

export function ensureNotificationConnection(): Promise<HubConnection> {
  if (startPromise) return startPromise;

  connection ??= build();
  const c = connection;

  startPromise = (async () => {
    if (c.state === HubConnectionState.Disconnected) {
      await c.start();
    }
    return c;
  })();

  return startPromise;
}

export async function shutdownNotificationConnection(): Promise<void> {
  startPromise = null;
  if (connection && connection.state !== HubConnectionState.Disconnected) {
    await connection.stop();
  }
  connection = null;
}
