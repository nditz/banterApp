import type { PredictionReaction } from '@/lib/reactionEngine';
import { getThemeForReaction } from '@/lib/predictionThemes';
import { cn } from '@/lib/utils';

export function PredictionReceiptCard({
  userName,
  fixture,
  pick,
  reaction,
  createdAt,
  probabilityContext,
  leagueName,
  className,
}: {
  userName: string;
  fixture: string;
  pick: string;
  reaction: PredictionReaction;
  createdAt: string;
  probabilityContext?: string;
  leagueName?: string;
  className?: string;
}) {
  const theme = getThemeForReaction(reaction.key);

  return (
    <div
      className={cn(
        'w-full max-w-md rounded-2xl bg-gradient-to-br from-brand via-brand-muted to-brand p-[1px] shadow-2xl',
        className
      )}
    >
      <div className="rounded-2xl bg-gradient-to-br from-brand to-brand-muted p-6 text-brand-foreground">
        <div className="text-[10px] font-bold uppercase tracking-[0.35em] text-brand-foreground/60">
          Prediction Receipt
        </div>
        <h2 className="mt-3 font-display text-2xl font-semibold">{userName} said:</h2>
        <div className="mt-4 rounded-xl bg-card p-4 text-card-foreground shadow-inner">
          <div className="text-xs font-bold uppercase tracking-wide text-muted-foreground">{fixture}</div>
          <div className="mt-1 font-display text-3xl font-semibold">{pick}</div>
          {probabilityContext && (
            <p className="mt-2 text-xs text-muted-foreground">{probabilityContext}</p>
          )}
        </div>
        <div className={cn('mt-4 text-xl font-semibold', theme.text)}>
          <span aria-hidden>{reaction.emoji.split('')[0]} </span>
          {reaction.title}
        </div>
        <p className="mt-1 text-sm text-brand-foreground/80">{reaction.selectedCaption}</p>
        <div className="mt-5 flex items-center justify-between border-t border-brand-foreground/15 pt-4 text-xs text-brand-foreground/70">
          <span>{createdAt}</span>
          <span className={cn('rounded-full px-2 py-0.5 font-bold', theme.bg, theme.text)}>
            {reaction.auraDelta > 0 ? '+' : ''}
            {reaction.auraDelta} aura
          </span>
        </div>
        {leagueName && (
          <p className="mt-2 text-[11px] text-brand-foreground/60">League: {leagueName}</p>
        )}
      </div>
    </div>
  );
}
