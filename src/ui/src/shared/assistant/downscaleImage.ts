/** Image types the server accepts (design §10). */
export const ACCEPTED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

/** Long edge the image is shrunk to: more pixels cost tokens without helping the model read a receipt. */
const MAX_EDGE = 1568;
const JPEG_QUALITY = 0.85;
/** Mirrors `Assistant:MaxImageBytes`. */
const MAX_BYTES = 2_000_000;

export interface AttachedImage {
  mediaType: 'image/jpeg';
  /** Base64, as the server expects it. */
  data: string;
  /** Object URL for the preview; revoke it when the image is sent or removed. */
  previewUrl: string;
}

export type DownscaleResult =
  { ok: true; image: AttachedImage } | { ok: false; reason: 'type' | 'size' | 'unreadable' };

/** Shrinks a picked or captured photo to at most {@link MAX_EDGE} px and re-encodes it as JPEG. */
export async function downscaleImage(file: File): Promise<DownscaleResult> {
  if (!ACCEPTED_IMAGE_TYPES.includes(file.type)) return { ok: false, reason: 'type' };

  let bitmap: ImageBitmap;
  try {
    bitmap = await createImageBitmap(file);
  } catch {
    return { ok: false, reason: 'unreadable' };
  }

  const scale = Math.min(1, MAX_EDGE / Math.max(bitmap.width, bitmap.height));
  const canvas = document.createElement('canvas');
  canvas.width = Math.round(bitmap.width * scale);
  canvas.height = Math.round(bitmap.height * scale);
  canvas.getContext('2d')?.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
  bitmap.close();

  const blob = await new Promise<Blob | null>((resolve) => {
    canvas.toBlob(resolve, 'image/jpeg', JPEG_QUALITY);
  });
  if (!blob) return { ok: false, reason: 'unreadable' };
  if (blob.size > MAX_BYTES) return { ok: false, reason: 'size' };

  const dataUrl = await new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      resolve(typeof reader.result === 'string' ? reader.result : '');
    };
    reader.onerror = () => {
      reject(reader.error ?? new Error('read failed'));
    };
    reader.readAsDataURL(blob);
  });

  return {
    ok: true,
    image: {
      mediaType: 'image/jpeg',
      data: dataUrl.slice(dataUrl.indexOf(',') + 1),
      previewUrl: URL.createObjectURL(blob),
    },
  };
}
