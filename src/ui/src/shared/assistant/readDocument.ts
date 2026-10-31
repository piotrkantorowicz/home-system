/** Bank statement exports the Budget import reads (design §10, #731). */
export const ACCEPTED_DOCUMENT_TYPES = ['.csv', '.xml', 'text/csv', 'text/xml', 'application/xml'];

export interface AttachedDocument {
  name: string;
  text: string;
}

/**
 * Reads a text file as the server's tools expect it: strict UTF-8 first, else Windows-1250, the
 * encoding Polish banks export in — the same order the Budget statement parser tries.
 */
export async function readDocument(file: File): Promise<AttachedDocument> {
  const bytes = await file.arrayBuffer();
  let text: string;
  try {
    text = new TextDecoder('utf-8', { fatal: true }).decode(bytes);
  } catch {
    text = new TextDecoder('windows-1250').decode(bytes);
  }
  return { name: file.name, text };
}
