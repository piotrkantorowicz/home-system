import { readEvents } from './readEvents';

import type { ServerEvent } from './readEvents';

function streamOf(...chunks: string[]): ReadableStream<Uint8Array> {
  const encoder = new TextEncoder();
  return new ReadableStream({
    start(controller) {
      for (const chunk of chunks) controller.enqueue(encoder.encode(chunk));
      controller.close();
    },
  });
}

async function collect(body: ReadableStream<Uint8Array>): Promise<ServerEvent[]> {
  const events: ServerEvent[] = [];
  for await (const event of readEvents(body)) events.push(event);
  return events;
}

describe('readEvents', () => {
  it('yields typed JSON events in order', async () => {
    const events = await collect(
      streamOf('event: text\ndata: {"text":"Hi"}\n\n', 'event: done\ndata: {}\n\n'),
    );

    expect(events).toEqual([
      { type: 'text', data: { text: 'Hi' } },
      { type: 'done', data: {} },
    ]);
  });

  it('joins an event split across chunks', async () => {
    const events = await collect(streamOf('event: te', 'xt\ndata: {"text":', '"Hi"}\r\n\r\n'));

    expect(events).toEqual([{ type: 'text', data: { text: 'Hi' } }]);
  });

  it('ignores comments and an unterminated tail', async () => {
    const events = await collect(streamOf(': keep-alive\n\nevent: text\ndata: {"text":"x"}'));

    expect(events).toEqual([]);
  });
});
