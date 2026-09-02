"use client";

import { useRef } from "react";
import { Camera } from "lucide-react";
import { UserAvatar } from "@/components/ui/UserAvatar";
import { AVATAR_ACCEPT } from "@/lib/avatar-image";
import { cn } from "@/lib/utils";

interface AvatarPickerProps {
  userId?: string;
  displayName: string;
  previewUrl?: string;
  busy?: boolean;
  disabled?: boolean;
  error?: string | null;
  size?: number;
  onFileChosen: (file: File) => void;
}

export function AvatarPicker({
  userId = "preview",
  displayName,
  previewUrl,
  busy = false,
  disabled = false,
  error,
  size = 72,
  onFileChosen,
}: AvatarPickerProps) {
  const inputRef = useRef<HTMLInputElement>(null);

  return (
    <div className="flex flex-col items-center gap-2">
      <button
        type="button"
        disabled={disabled || busy}
        onClick={() => inputRef.current?.click()}
        className={cn(
          "relative cursor-pointer rounded-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/60 disabled:cursor-not-allowed disabled:opacity-60"
        )}
        aria-label="Choose profile photo"
      >
        <UserAvatar
          userId={userId}
          displayName={displayName}
          avatarUrl={previewUrl}
          size={size}
          className="block"
        />
        <span className="absolute inset-x-0 bottom-0 flex items-center justify-center rounded-b-full bg-black/45 py-1">
          <Camera className="size-3.5 text-white" aria-hidden />
        </span>
      </button>
      <input
        ref={inputRef}
        type="file"
        accept={AVATAR_ACCEPT}
        className="sr-only"
        disabled={disabled || busy}
        onChange={(event) => {
          const file = event.target.files?.[0];
          event.target.value = "";
          if (file) onFileChosen(file);
        }}
      />
      <p className="text-center text-[11px] text-muted-foreground">
        {busy ? "Saving photo…" : "Optional photo · we'll shrink it automatically"}
      </p>
      {error ? (
        <p className="text-center text-xs text-destructive" role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
}
