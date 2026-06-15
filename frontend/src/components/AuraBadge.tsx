import { auraLevels } from '@/reactions/reactionContent';
import { cn } from '@/lib/utils';

export function AuraBadge({ aura, className }: { aura: number; className?: string }) {
  const level = [...auraLevels].reverse().find((item) => aura >= item.min) ?? auraLevels[0];

  return (
    <div
      className={cn(
        'inline-flex items-center gap-2 rounded-full border border-aura/40 bg-aura/10 px-3 py-1.5 text-xs font-bold text-foreground shadow-sm aura-badge-glow',
        className
      )}
    >
      <span aria-hidden>{level.emoji}</span>
      <span className="text-aura">{level.label}</span>
      <span className="font-mono text-muted-foreground">{aura} aura</span>
    </div>
  );
}
