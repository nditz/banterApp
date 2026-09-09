import { auraLevels } from '@/reactions/reactionContent';

export function AuraBadge({ aura }: { aura: number }) {
  const level = [...auraLevels].reverse().find((item) => aura >= item.min) ?? auraLevels[0];

  return (
    <div className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/10 px-4 py-2 text-sm font-bold text-white">
      <span>{level.emoji}</span>
      <span>{level.label}</span>
      <span className="text-white/60">{aura} aura</span>
    </div>
  );
}
