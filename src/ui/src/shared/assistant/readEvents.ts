export interface ServerEvent {
  type: string;
  data: unknown;
}

/**
 * Reads a `text/event-stream` body and yields each event as it completes. Only the `event:` and
 * `data:` fields are used (the assistant sends nothing else); `data` is parsed as JSON.
 */
export async function* readEvents(body: ReadableStream<Uint8Array>): AsyncGenerator<ServerEvent> {
  const reader = body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  for (;;) {
    const { value, done } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true }).replace(/\r\n?/g, '\n');
    let end = buffer.indexOf('\n\n');
    while (end !== -1) {
      const event = parse(buffer.slice(0, end));
      buffer = buffer.slice(end + 2);
      if (event) yield event;
      end = buffer.indexOf('\n\n');
    }
  }
}

function parse(block: string): ServerEvent | null {
  let type = 'message';
  const data: string[] = [];
  for (const line of block.split('\n')) {
    const colon = line.indexOf(':');
    const field = colon === -1 ? line : line.slice(0, colon);
    const value = colon === -1 ? '' : line.slice(colon + 1).replace(/^ /, '');
    if (field === 'event') type = value;
    else if (field === 'data') data.push(value);
  }
  return data.length > 0 ? { type, data: JSON.parse(data.join('\n')) as unknown } : null;
}
