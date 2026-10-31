import { readDocument } from './readDocument';

describe('readDocument', () => {
  it('reads a UTF-8 file as is', async () => {
    const file = new File(['Żabka;-12,50'], 'utf8.csv', { type: 'text/csv' });

    expect(await readDocument(file)).toEqual({ name: 'utf8.csv', text: 'Żabka;-12,50' });
  });

  it('falls back to Windows-1250 for a Polish bank export', async () => {
    // "Żabka" in Windows-1250: Ż is 0xAF, which is not valid UTF-8 on its own.
    const file = new File([new Uint8Array([0xaf, 0x61, 0x62, 0x6b, 0x61])], 'mbank.csv');

    expect((await readDocument(file)).text).toBe('Żabka');
  });
});
