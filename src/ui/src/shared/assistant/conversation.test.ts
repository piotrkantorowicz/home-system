import { loadConversation, recentTurns, saveConversation } from './conversation';

import type { AssistantEntry, Conversation } from './conversation';

function turn(n: number, size = 10): { history: unknown[]; entries: AssistantEntry[] } {
  return {
    history: [
      { role: 'user', contents: [{ $type: 'text', text: `q${String(n)}` }] },
      { role: 'assistant', contents: [{ $type: 'functionCall', name: 'list_wallets' }] },
      { role: 'tool', contents: [{ $type: 'functionResult', result: 'x'.repeat(size) }] },
      { role: 'assistant', contents: [{ $type: 'text', text: `a${String(n)}` }] },
    ],
    entries: [
      { role: 'user', text: `q${String(n)}` },
      { role: 'assistant', text: `a${String(n)}`, tools: [] },
    ],
  };
}

function conversationOf(...turns: ReturnType<typeof turn>[]): Conversation {
  return {
    history: turns.flatMap((t) => t.history),
    entries: turns.flatMap((t) => t.entries),
  };
}

describe('recentTurns', () => {
  it('drops the oldest turns whole, keeping entries for the same turns', () => {
    const one = turn(1);
    const turnSize = one.history.reduce<number>((n, m) => n + JSON.stringify(m).length, 0);

    const recent = recentTurns(conversationOf(one, turn(2), turn(3)), turnSize * 2);

    expect(recent.history).toEqual([...turn(2).history, ...turn(3).history]);
    expect(recent.entries.map((e) => e.text)).toEqual(['q2', 'a2', 'q3', 'a3']);
  });

  it('keeps the latest turn even when it alone is over the allowance', () => {
    const recent = recentTurns(conversationOf(turn(1), turn(2, 500)), 100);

    expect(recent.history).toEqual(turn(2, 500).history);
    expect(recent.entries.map((e) => e.text)).toEqual(['q2', 'a2']);
  });

  it('leaves a conversation that fits unchanged', () => {
    const conversation = conversationOf(turn(1), turn(2));

    expect(recentTurns(conversation, 1_000_000)).toEqual(conversation);
  });
});

describe('saveConversation', () => {
  it('stores only the turns the server would still read', () => {
    window.localStorage.clear();
    const turns = Array.from({ length: 30 }, (_, i) => turn(i, 5_000));

    saveConversation('alex', conversationOf(...turns));

    const stored = loadConversation('alex');
    expect(stored.entries.length).toBeLessThan(60);
    expect(stored.entries.at(-1)?.text).toBe('a29');
  });
});
