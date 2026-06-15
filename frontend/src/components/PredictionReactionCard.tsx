'use client';

import Image from 'next/image';
import { motion, useReducedMotion } from 'framer-motion';
import { motionEase } from '@/lib/motionConfig';
import { getThemeForReaction } from '@/lib/predictionThemes';
import type { PredictionReaction } from '@/lib/reactionEngine';
import { cn } from '@/lib/utils';

export function PredictionReactionCard({
  reaction,
  className,
  animate = true,
}: {
  reaction: PredictionReaction;
  className?: string;
  /** Set false for static/history lists to skip entrance motion */
  animate?: boolean;
}) {
  const reduceMotion = useReducedMotion();
  const theme = getThemeForReaction(reaction.key);
  const shouldAnimate = animate && !reduceMotion;

  return (
    <motion.div
      initial={shouldAnimate ? { opacity: 0, y: 20, scale: 0.95 } : false}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={
        shouldAnimate
          ? { duration: 0.65, ease: motionEase }
          : { duration: 0 }
      }
      className={cn(
        'reaction-card-themed shadow-sm',
        theme.border,
        !reduceMotion && theme.glow,
        className
      )}
    >
      <div className="flex items-center gap-3">
        <div className={cn(
          'relative h-16 w-16 shrink-0 overflow-hidden rounded-xl ring-1',
          theme.border,
          theme.bg
        )}>
          <Image
            src={reaction.asset}
            alt={reaction.title}
            fill
            className="object-cover"
            unoptimized
          />
        </div>
        <div className="min-w-0">
          <div className={cn('text-[10px] font-bold uppercase tracking-[0.2em]', theme.text)}>
            {reaction.archetype}
          </div>
          <h3 className="mt-0.5 text-base font-bold leading-tight text-foreground">
            <span aria-hidden>{reaction.emoji.split('')[0]} </span>
            {reaction.title}
          </h3>
          <p className="mt-1 text-sm leading-snug text-muted-foreground">{reaction.selectedCaption}</p>
        </div>
      </div>
      <div className="mt-3 flex flex-wrap gap-1.5">
        {reaction.microcopy.map((item) => (
          <span
            key={item}
            className="rounded-full bg-muted/80 px-2.5 py-0.5 text-[10px] font-semibold text-muted-foreground"
          >
            {item}
          </span>
        ))}
        <span className={cn(
          'rounded-full px-2.5 py-0.5 text-[10px] font-bold',
          theme.bg,
          theme.text
        )}>
          {reaction.auraDelta > 0 ? '+' : ''}
          {reaction.auraDelta} aura
        </span>
      </div>
    </motion.div>
  );
}
