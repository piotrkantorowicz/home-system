/**
 * This file would normally be generated via `npm run generate:api:notifications`.
 * Hand-written placeholder until the backend is run locally and regenerated.
 */

export interface paths {
  '/api/notifications': {
    get: {
      parameters: {
        query?: {
          page?: number;
          pageSize?: number;
        };
      };
      responses: {
        200: {
          content: {
            'application/json': components['schemas']['PagedListOfNotificationDto'];
          };
        };
      };
    };
  };
  '/api/notifications/{id}/read': {
    post: {
      parameters: {
        path: {
          id: string;
        };
      };
      responses: {
        204: { content: never };
        404: { content: never };
      };
    };
  };
  '/api/notification-preferences': {
    get: {
      responses: {
        200: {
          content: {
            'application/json': components['schemas']['ChannelPreferencesDto'];
          };
        };
      };
    };
    put: {
      requestBody: {
        content: {
          'application/json': components['schemas']['ChannelPreferencesDto'];
        };
      };
      responses: {
        204: { content: never };
        400: { content: never };
      };
    };
  };
}

export interface components {
  schemas: {
    NotificationDto: {
      id?: string;
      type?: string;
      title?: string;
      body?: string;
      createdAt?: string;
      readAt?: string | null;
    };
    PagedListOfNotificationDto: {
      items?: components['schemas']['NotificationDto'][];
      page?: number;
      pageSize?: number;
      totalCount?: number;
      totalPages?: number;
    };
    ChannelPreferencesDto: {
      consoleEnabled: boolean;
      emailEnabled: boolean;
      webSocketEnabled: boolean;
    };
  };
}
