'use client';

import Image from 'next/image';
import { motion } from 'framer-motion';
import type { PredictionReaction } from '@/lib/reactionEngine';

export function PredictionReactionCard({ reaction }: { reaction: PredictionReaction }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 16, scale: 0.96 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={{ type: 'spring', stiffness: 260, damping: 20 }}
      className="rounded-3xl border border-white/10 bg-black/70 p-5 shadow-2xl backdrop-blur"
    >
      <div className="flex items-center gap-4">
        <div className="relative h-20 w-20 overflow-hidden rounded-2xl bg-white/5">
          <Image src={reaction.asset} alt={reaction.title} fill className="object-cover" unoptimized />
        </div>
        <div>
          <div className="text-sm uppercase tracking-[0.25em] text-white/50">{reaction.archetype}</div>
          <h3 className="text-2xl font-black text-white">{reaction.emoji} {reaction.title}</h3>
          <p className="mt-1 text-white/80">{reaction.selectedCaption}</p>
        </div>
      </div>
      <div className="mt-4 flex flex-wrap gap-2">
        {reaction.microcopy.map((item) => (
          <span key={item} className="rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-white/80">
            {item}
          </span>
        ))}
        <span className="rounded-full bg-white px-3 py-1 text-xs font-black text-black">
          {reaction.auraDelta > 0 ? '+' : ''}{reaction.auraDelta} aura
        </span>
      </div>
    </motion.div>
  );
}
