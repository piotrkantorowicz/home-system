import { initI18n } from '@shared/lib/i18n';
import { QueryClient } from '@tanstack/react-query';
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';

import { AssistantPanel, OPEN_ASSISTANT_EVENT } from './AssistantPanel';

import { server } from '@/test/mocks/server';
import { createWrapper } from '@/test/utils/queryWrapper';

const unloadHandlers = vi.hoisted(() => new Set<() => void>());

vi.mock('@shared/auth/userManager', () => ({
  userManager: {
    events: {
      addUserUnloaded: (h: () => void) => unloadHandlers.add(h),
      removeUserUnloaded: (h: () => void) => unloadHandlers.delete(h),
    },
  },
}));
vi.mock('@shared/api/tokenInterceptor', () => ({
  getValidToken: vi.fn().mockResolvedValue('token'),
  tryRenewToken: vi.fn(),
  redirectToLogin: vi.fn(),
}));

const BASE = 'http://localhost:5050/api/assistant';
const encoder = new TextEncoder();

function sse(type: string, data: unknown): Uint8Array {
  return encoder.encode(`event: ${type}\ndata: ${JSON.stringify(data)}\n\n`);
}

function eventStream(...chunks: Uint8Array[]): HttpResponse<ReadableStream> {
  return new HttpResponse(
    new ReadableStream({
      start(controller) {
        for (const chunk of chunks) controller.enqueue(chunk);
        controller.close();
      },
    }),
    { headers: { 'Content-Type': 'text/event-stream' } },
  );
}

function renderPanel(subject = 'alex', client = new QueryClient()) {
  return render(<AssistantPanel subject={subject} />, { wrapper: createWrapper(client) });
}

// The panel opens as soon as the status query has answered.
async function openPanel() {
  act(() => {
    window.dispatchEvent(new Event(OPEN_ASSISTANT_EVENT));
  });
  return screen.findByRole('dialog', { name: 'Assistant' });
}

async function ask(text: string) {
  await userEvent.type(screen.getByRole('textbox', { name: 'Message to the assistant' }), text);
  await userEvent.click(screen.getByRole('button', { name: 'Send' }));
}

beforeAll(() => {
  initI18n([]);
});

beforeEach(() => {
  window.localStorage.clear();
  server.use(http.get(BASE, () => HttpResponse.json({ provider: 'Anthropic' })));
});

describe('AssistantPanel', () => {
  it('renders the streamed answer as it arrives, with a line per tool call', async () => {
    let push: (chunk: Uint8Array) => void = () => undefined;
    let finish: () => void = () => undefined;
    server.use(
      http.post(
        `${BASE}/messages`,
        () =>
          new HttpResponse(
            new ReadableStream({
              start(controller) {
                push = (chunk) => {
                  controller.enqueue(chunk);
                };
                finish = () => {
                  controller.close();
                };
              },
            }),
            { headers: { 'Content-Type': 'text/event-stream' } },
          ),
      ),
    );
    renderPanel();
    await openPanel();

    await ask('What wallets do I have?');
    act(() => {
      push(sse('tool_call', { name: 'list_wallets', summary: '' }));
      push(sse('tool_result', { name: 'list_wallets', ok: true }));
      push(sse('text', { text: 'You have ' }));
    });

    expect(await screen.findByText('You have')).toBeInTheDocument();
    expect(screen.getByText('list_wallets')).toBeInTheDocument();
    expect(screen.getByLabelText('Done')).toBeInTheDocument();

    act(() => {
      push(sse('text', { text: 'one wallet.' }));
      push(sse('done', {}));
      finish();
    });

    expect(await screen.findByText('You have one wallet.')).toBeInTheDocument();
  });

  it('sends the stored history and time zone with the next message', async () => {
    let body: unknown;
    server.use(
      http.post(`${BASE}/messages`, async ({ request }) => {
        body = await request.json();
        return eventStream(
          sse('text', { text: 'Hi' }),
          sse('history', { messages: [{ role: 'user' }, { role: 'assistant' }] }),
          sse('done', {}),
        );
      }),
    );
    renderPanel();
    await openPanel();

    await ask('Hello');
    await screen.findByText('Hi');
    await ask('Again');

    await vi.waitFor(() => {
      expect(body).toMatchObject({
        message: 'Again',
        messages: [{ role: 'user' }, { role: 'assistant' }],
        timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      });
    });
  });

  it('restores the conversation after a reload and clear removes it', async () => {
    server.use(
      http.post(`${BASE}/messages`, () =>
        eventStream(sse('text', { text: 'Stored answer' }), sse('done', {})),
      ),
    );
    const first = renderPanel();
    await openPanel();
    await ask('Remember me');
    await screen.findByText('Stored answer');
    first.unmount();

    renderPanel();
    await openPanel();
    expect(screen.getByText('Remember me')).toBeInTheDocument();
    expect(screen.getByText('Stored answer')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Clear conversation' }));

    expect(screen.queryByText('Stored answer')).not.toBeInTheDocument();
    expect(window.localStorage.getItem('home-system-assistant:alex')).toBeNull();
  });

  it('shows a readable message when the limit is reached', async () => {
    server.use(
      http.post(`${BASE}/messages`, () =>
        HttpResponse.json({ title: 'Assistant limit reached' }, { status: 429 }),
      ),
    );
    renderPanel();
    await openPanel();

    await ask('Hi');

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'You have reached the assistant limit. Try again later.',
    );
  });

  it('starts empty for the next user after sign-out', async () => {
    server.use(
      http.post(`${BASE}/messages`, () =>
        eventStream(sse('text', { text: 'Private answer' }), sse('done', {})),
      ),
    );
    const first = renderPanel('user-a');
    await openPanel();
    await ask('My secret');
    await screen.findByText('Private answer');

    act(() => {
      for (const handler of unloadHandlers) handler();
    });
    first.unmount();
    renderPanel('user-b');
    await openPanel();

    expect(screen.getByText('Ask about your meals, budget or investments.')).toBeInTheDocument();
    expect(window.localStorage.getItem('home-system-assistant:user-a')).toBeNull();
  });

  it('renders nothing while the assistant is switched off', async () => {
    server.use(http.get(BASE, () => new HttpResponse(null, { status: 404 })));
    const client = new QueryClient();
    renderPanel('alex', client);

    act(() => {
      window.dispatchEvent(new Event(OPEN_ASSISTANT_EVENT));
    });

    await vi.waitFor(() => {
      expect(client.getQueryState(['assistant', 'status'])?.status).toBe('success');
    });
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});
