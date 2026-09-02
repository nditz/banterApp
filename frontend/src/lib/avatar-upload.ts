import { apiFetch } from "@/lib/api";
import {
  dataUrlToBlob,
  getAvatarFileError,
  shrinkAvatarImage,
  shrinkAvatarToDataUrl,
} from "@/lib/avatar-image";
import { getSupabaseAvatarUrl } from "@/lib/avatars";
import { createClient } from "@/lib/supabase/client";

const PENDING_AVATAR_KEY = "banter_pending_avatar";

export async function syncAuthSession(avatarUrl?: string | null): Promise<void> {
  await apiFetch("/api/auth/session/sync", {
    method: "POST",
    body: JSON.stringify(avatarUrl ? { avatarUrl } : {}),
  });
}

export function stashPendingAvatarDataUrl(dataUrl: string): void {
  try {
    sessionStorage.setItem(PENDING_AVATAR_KEY, dataUrl);
  } catch {
    // Private mode / quota — photo is optional.
  }
}

export function clearPendingAvatar(): void {
  try {
    sessionStorage.removeItem(PENDING_AVATAR_KEY);
  } catch {
    /* ignore */
  }
}

function takePendingAvatarDataUrl(): string | null {
  try {
    const value = sessionStorage.getItem(PENDING_AVATAR_KEY);
    if (value) sessionStorage.removeItem(PENDING_AVATAR_KEY);
    return value;
  } catch {
    return null;
  }
}

export async function prepareAvatarPreview(file: File): Promise<string> {
  const error = getAvatarFileError(file);
  if (error) throw new Error(error);
  return shrinkAvatarToDataUrl(file);
}

export async function uploadAvatarBlob(userId: string, blob: Blob): Promise<string> {
  const supabase = createClient();
  if (!supabase) {
    throw new Error("Supabase is not configured.");
  }

  const contentType = blob.type === "image/jpeg" ? "image/jpeg" : "image/webp";
  const ext = contentType === "image/jpeg" ? "jpg" : "webp";
  const path = `${userId}/avatar.${ext}`;

  const { error: uploadError } = await supabase.storage.from("avatars").upload(path, blob, {
    upsert: true,
    contentType,
    cacheControl: "3600",
  });

  if (uploadError) {
    throw new Error(
      uploadError.message.includes("Bucket not found")
        ? "Photo storage isn't set up yet. You can add a picture later."
        : uploadError.message
    );
  }

  const {
    data: { publicUrl },
  } = supabase.storage.from("avatars").getPublicUrl(path);
  const avatarUrl = `${publicUrl}?v=${Date.now()}`;

  await supabase.auth.updateUser({ data: { avatar_url: avatarUrl } });
  await supabase.from("profiles").update({ avatar_url: avatarUrl }).eq("id", userId);

  try {
    await syncAuthSession(avatarUrl);
  } catch {
    // Header still uses the Supabase user photo even if backend sync fails.
  }

  return avatarUrl;
}

export async function uploadAvatarFile(userId: string, file: File): Promise<string> {
  const error = getAvatarFileError(file);
  if (error) throw new Error(error);
  const blob = await shrinkAvatarImage(file);
  return uploadAvatarBlob(userId, blob);
}

export async function flushPendingAvatar(userId: string): Promise<string | null> {
  const dataUrl = takePendingAvatarDataUrl();
  if (!dataUrl) return null;
  try {
    const blob = dataUrlToBlob(dataUrl);
    return await uploadAvatarBlob(userId, blob);
  } catch {
    stashPendingAvatarDataUrl(dataUrl);
    return null;
  }
}

export async function currentSupabaseAvatarUrl(): Promise<string | undefined> {
  const supabase = createClient();
  if (!supabase) return undefined;
  const { data } = await supabase.auth.getUser();
  return data.user ? getSupabaseAvatarUrl(data.user) : undefined;
}
