export const AVATAR_MAX_INPUT_BYTES = 8 * 1024 * 1024;
export const AVATAR_MAX_DIMENSION = 512;
export const AVATAR_OUTPUT_QUALITY = 0.82;
export const AVATAR_ACCEPT = "image/jpeg,image/png,image/webp,image/heic,image/heif";

export function getAvatarFileError(file: File): string | null {
  if (!file.type.startsWith("image/")) {
    return "Pick a photo (JPG, PNG, or WebP).";
  }
  if (file.size > AVATAR_MAX_INPUT_BYTES) {
    return "That photo is over 8 MB. Pick a smaller one — we'll shrink it from there.";
  }
  return null;
}

function blobToDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result === "string") resolve(reader.result);
      else reject(new Error("Could not read photo."));
    };
    reader.onerror = () => reject(new Error("Could not read photo."));
    reader.readAsDataURL(blob);
  });
}

function loadImageBitmap(file: Blob): Promise<ImageBitmap> {
  return createImageBitmap(file);
}

/**
 * Downscale and recompress a photo in the browser so avatars stay small
 * (max 512px, WebP/JPEG). No extra library — Canvas is enough here.
 */
export async function shrinkAvatarImage(file: Blob): Promise<Blob> {
  const bitmap = await loadImageBitmap(file);
  const scale = Math.min(1, AVATAR_MAX_DIMENSION / Math.max(bitmap.width, bitmap.height));
  const width = Math.max(1, Math.round(bitmap.width * scale));
  const height = Math.max(1, Math.round(bitmap.height * scale));

  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext("2d");
  if (!ctx) {
    bitmap.close();
    throw new Error("Could not process that photo.");
  }
  ctx.drawImage(bitmap, 0, 0, width, height);
  bitmap.close();

  const blob = await new Promise<Blob | null>((resolve) => {
    canvas.toBlob(resolve, "image/webp", AVATAR_OUTPUT_QUALITY);
  });

  if (blob && blob.size > 0) return blob;

  const jpeg = await new Promise<Blob | null>((resolve) => {
    canvas.toBlob(resolve, "image/jpeg", AVATAR_OUTPUT_QUALITY);
  });
  if (jpeg && jpeg.size > 0) return jpeg;

  throw new Error("Couldn't read that image. Try a JPG or PNG.");
}

export async function shrinkAvatarToDataUrl(file: File): Promise<string> {
  const error = getAvatarFileError(file);
  if (error) throw new Error(error);
  const blob = await shrinkAvatarImage(file);
  return blobToDataUrl(blob);
}

export function dataUrlToBlob(dataUrl: string): Blob {
  const [header, data] = dataUrl.split(",");
  if (!header || !data) throw new Error("Invalid photo data.");
  const mime = header.match(/data:(.*?);/)?.[1] ?? "image/jpeg";
  const binary = atob(data);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return new Blob([bytes], { type: mime });
}
