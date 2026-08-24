"use client";

import { motion, useReducedMotion } from "framer-motion";
import { ChevronDown, Copy, KeyRound } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { UsernameSetup } from "@/components/session/UsernameSetup";
import { Button } from "@/components/ui/button";
import { motionEase } from "@/lib/motionConfig";
import { cn } from "@/lib/utils";

const PREVIEW_STORAGE_KEY = "banter_recovery_key_preview";

interface RecoveryKeyPanelProps {
  recoveryToken: string;
  username?: string | null;
}

export function RecoveryKeyPanel({ recoveryToken, username }: RecoveryKeyPanelProps) {
  const reduceMotion = useReducedMotion();
  const [expanded, setExpanded] = useState(false);
  const [copied, setCopied] = useState(false);
  const [previewing, setPreviewing] = useState(false);
  const timers = useRef<number[]>([]);

  const clearPreviewTimers = () => {
    timers.current.forEach((id) => window.clearTimeout(id));
    timers.current = [];
  };

  const markPreviewSeen = () => {
    try {
      sessionStorage.setItem(PREVIEW_STORAGE_KEY, "1");
    } catch {
      /* ignore quota / private mode */
    }
  };

  useEffect(() => {
    if (reduceMotion === null) return;

    let seen = false;
    try {
      seen = sessionStorage.getItem(PREVIEW_STORAGE_KEY) === "1";
    } catch {
      seen = false;
    }

    if (seen) return;

    if (reduceMotion) {
      markPreviewSeen();
      return;
    }

    const startId = window.setTimeout(() => setPreviewing(true), 0);
    const expandId = window.setTimeout(() => setExpanded(true), 420);
    const collapseId = window.setTimeout(() => {
      setExpanded(false);
      setPreviewing(false);
      markPreviewSeen();
    }, 3200);
    timers.current = [startId, expandId, collapseId];

    return () => {
      clearPreviewTimers();
    };
  }, [reduceMotion]);

  const toggle = () => {
    clearPreviewTimers();
    setPreviewing(false);
    markPreviewSeen();
    setExpanded((open) => !open);
  };

  const copyKey = async () => {
    await navigator.clipboard.writeText(recoveryToken);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div
      className={cn(
        "mx-auto mb-4 max-w-[1200px] overflow-hidden rounded-md border border-gold/30 bg-gold/5 transition-[box-shadow] duration-500",
        previewing && "ring-2 ring-gold/40 ring-offset-2 ring-offset-background"
      )}
    >
      <button
        type="button"
        className="flex w-full items-center gap-2.5 px-4 py-2.5 text-left"
        aria-expanded={expanded}
        aria-controls="recovery-key-details"
        onClick={toggle}
      >
        <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-gold/20">
          <KeyRound className="size-3.5 text-gold" aria-hidden />
        </span>
        <span className="min-w-0 flex-1">
          <span className="block text-xs font-semibold text-foreground">Recovery key</span>
          <span className="block text-[11px] text-muted-foreground">
            {expanded ? "Copy this key, then keep it somewhere safe." : "Tap to copy your session key."}
          </span>
        </span>
        <ChevronDown
          className={cn(
            "size-4 shrink-0 text-muted-foreground transition-transform duration-300",
            expanded && "rotate-180"
          )}
          aria-hidden
        />
      </button>

      <motion.div
        id="recovery-key-details"
        initial={false}
        animate={{ gridTemplateRows: expanded ? "1fr" : "0fr" }}
        transition={{ duration: reduceMotion ? 0 : 0.45, ease: motionEase }}
        className="grid [grid-template-rows:0fr]"
      >
        <div className="min-h-0 overflow-hidden">
          <div className="space-y-3 px-4 pb-3" inert={!expanded || undefined}>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-[11px] text-muted-foreground">
                Use it to restore your session if you clear cookies or switch devices.
              </p>
              <Button type="button" size="sm" variant="outline" onClick={copyKey}>
                <Copy className="size-3.5" aria-hidden />
                {copied ? "Copied" : "Copy key"}
              </Button>
            </div>
            <code className="block overflow-x-auto rounded-md bg-card px-2 py-1.5 text-[10px] text-muted-foreground">
              {recoveryToken}
            </code>
            {expanded && (
              <div className="border-t border-gold/20 pt-3">
                <UsernameSetup initialUsername={username} compact />
              </div>
            )}
          </div>
        </div>
      </motion.div>
    </div>
  );
}
