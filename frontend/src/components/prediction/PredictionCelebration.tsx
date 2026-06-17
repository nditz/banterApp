"use client";

import { motion, useReducedMotion } from "framer-motion";
import Image from "next/image";
import { Receipt } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { PredictionReceiptCard } from "@/components/PredictionReceiptCard";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  celebrationSpring,
  celebrationTiming,
  motionEase,
} from "@/lib/motionConfig";
import { getThemeForReaction } from "@/lib/predictionThemes";
import type { PredictionReaction } from "@/lib/reactionEngine";
import { cn } from "@/lib/utils";

interface PredictionCelebrationProps {
  reaction: PredictionReaction;
  userName: string;
  fixture: string;
  pick: string;
  probabilityContext?: string;
  leagueName?: string;
}

function formatReceiptTimestamp(date = new Date()): string {
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

export function PredictionCelebration({
  reaction,
  userName,
  fixture,
  pick,
  probabilityContext,
  leagueName,
}: PredictionCelebrationProps) {
  const reduceMotion = useReducedMotion();
  const containerRef = useRef<HTMLDivElement>(null);
  const [receiptOpen, setReceiptOpen] = useState(false);
  const theme = getThemeForReaction(reaction.key);
  const createdAt = formatReceiptTimestamp();
  const emoji = reaction.emoji.split("")[0] ?? "⚽";

  useEffect(() => {
    if (reduceMotion) return;
    const timer = setTimeout(() => {
      containerRef.current?.scrollIntoView({
        behavior: "smooth",
        block: "nearest",
      });
    }, 120);
    return () => clearTimeout(timer);
  }, [reduceMotion, reaction.key, pick]);

  const instant = reduceMotion
    ? { duration: 0 }
    : { duration: celebrationTiming.flashDuration, ease: motionEase };

  const cardTransition = reduceMotion
    ? { duration: 0 }
    : {
        delay: celebrationTiming.cardDelay,
        duration: celebrationTiming.cardDuration,
        ease: motionEase,
      };

  return (
    <div
      ref={containerRef}
      className="space-y-3 pt-1"
      role="status"
      aria-live="polite"
    >
      {/* Confirmation flash — stays until the next pick replaces this block */}
      <motion.div
        initial={reduceMotion ? false : { opacity: 0, y: 16, scale: 0.92 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        transition={instant}
        className={cn(
          "flex items-center gap-3 rounded-xl border px-4 py-3",
          theme.border,
          theme.bg,
          !reduceMotion && theme.glow
        )}
      >
        <motion.span
          className={cn("text-2xl leading-none", theme.text)}
          initial={reduceMotion ? false : { scale: 0, rotate: -25 }}
          animate={{ scale: 1, rotate: 0 }}
          transition={
            reduceMotion
              ? { duration: 0 }
              : {
                  type: "spring",
                  stiffness: 220,
                  damping: 12,
                  mass: 0.8,
                }
          }
          aria-hidden
        >
          {emoji}
        </motion.span>
        <div className="min-w-0">
          <p className={cn("text-sm font-bold", theme.text)}>Receipt secured.</p>
          <p className="text-xs text-muted-foreground">Your football knowledge is on record.</p>
        </div>
      </motion.div>

      {/* Reaction card — persistent */}
      <motion.div
        initial={reduceMotion ? false : { opacity: 0, y: 28, scale: 0.94 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        transition={cardTransition}
        className={cn(
          "reaction-card-themed relative overflow-hidden shadow-md",
          theme.border,
          !reduceMotion && theme.glow
        )}
      >
        {!reduceMotion && (
          <motion.div
            className="pointer-events-none absolute inset-0 rounded-xl border-2 border-current opacity-0"
            style={{ color: `var(--${theme.id === "safe" ? "safe" : theme.id})` }}
            initial={{ opacity: 0 }}
            animate={{ opacity: [0, 0.7, 0.25, 0.6, 0.2] }}
            transition={{
              duration: celebrationTiming.glowPulseDuration,
              times: [0, 0.15, 0.35, 0.55, 0.75],
              delay: celebrationTiming.cardDelay,
            }}
            aria-hidden
          />
        )}

        <div className="relative flex items-center gap-3.5 p-4">
          <motion.div
            className={cn(
              "relative h-20 w-20 shrink-0 overflow-hidden rounded-xl ring-2",
              theme.border,
              theme.bg
            )}
            initial={reduceMotion ? false : { scale: 0.6, rotate: -8 }}
            animate={{ scale: 1, rotate: 0 }}
            transition={
              reduceMotion
                ? { duration: 0 }
                : {
                    ...celebrationSpring,
                    delay: celebrationTiming.cardDelay + 0.1,
                  }
            }
          >
            <Image
              src={reaction.asset}
              alt={reaction.title}
              fill
              className="object-cover"
              unoptimized
            />
          </motion.div>
          <div className="min-w-0">
            <div
              className={cn(
                "text-[10px] font-bold uppercase tracking-[0.2em]",
                theme.text
              )}
            >
              {reaction.archetype}
            </div>
            <h3 className="mt-0.5 font-display text-lg font-bold leading-tight text-foreground">
              {reaction.title}
            </h3>
            <p className="mt-1.5 text-sm leading-snug text-muted-foreground">
              {reaction.selectedCaption}
            </p>
          </div>
        </div>

        <motion.div
          className="relative flex flex-wrap gap-1.5 border-t border-border/60 px-4 py-3"
          initial={reduceMotion ? false : { opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={
            reduceMotion
              ? { duration: 0 }
              : {
                  delay: celebrationTiming.cardDelay + 0.35,
                  duration: 0.45,
                }
          }
        >
          {reaction.microcopy.map((item) => (
            <span
              key={item}
              className="rounded-full bg-muted/80 px-2.5 py-0.5 text-[10px] font-semibold text-muted-foreground"
            >
              {item}
            </span>
          ))}
          <span
            className={cn(
              "rounded-full px-2.5 py-0.5 text-[10px] font-bold",
              theme.bg,
              theme.text
            )}
          >
            {reaction.auraDelta > 0 ? "+" : ""}
            {reaction.auraDelta} aura
          </span>
        </motion.div>
      </motion.div>

      <motion.div
        initial={reduceMotion ? false : { opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={
          reduceMotion
            ? { duration: 0 }
            : {
                delay: celebrationTiming.cardDelay + 0.55,
                duration: 0.45,
                ease: motionEase,
              }
        }
      >
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="h-9 w-full cursor-pointer border-aura/30 text-xs hover:bg-aura/10"
          onClick={() => setReceiptOpen(true)}
        >
          <Receipt className="size-3.5" aria-hidden />
          Share receipt
        </Button>
      </motion.div>

      <Dialog open={receiptOpen} onOpenChange={setReceiptOpen}>
        <DialogContent className="max-w-md border-none bg-transparent p-0 shadow-none ring-0 sm:max-w-md">
          <DialogHeader className="sr-only">
            <DialogTitle>Prediction receipt</DialogTitle>
          </DialogHeader>
          <PredictionReceiptCard
            userName={userName}
            fixture={fixture}
            pick={pick}
            reaction={reaction}
            createdAt={createdAt}
            probabilityContext={probabilityContext}
            leagueName={leagueName}
          />
        </DialogContent>
      </Dialog>
    </div>
  );
}
