import type { PredictionReaction } from '@/lib/reactionEngine';

export function PredictionReceiptCard({
  userName,
  fixture,
  pick,
  reaction,
  createdAt
}: {
  userName: string;
  fixture: string;
  pick: string;
  reaction: PredictionReaction;
  createdAt: string;
}) {
  return (
    <div className="w-full max-w-md rounded-[2rem] bg-gradient-to-br from-zinc-950 to-zinc-800 p-6 text-white shadow-2xl">
      <div className="text-xs uppercase tracking-[0.35em] text-white/50">Prediction Receipt</div>
      <h2 className="mt-4 text-3xl font-black">{userName} said:</h2>
      <div className="mt-5 rounded-2xl bg-white p-5 text-black">
        <div className="text-sm font-bold uppercase text-black/50">{fixture}</div>
        <div className="mt-2 text-4xl font-black">{pick}</div>
      </div>
      <div className="mt-5 text-2xl font-black">{reaction.emoji} {reaction.title}</div>
      <p className="mt-2 text-white/75">{reaction.selectedCaption}</p>
      <div className="mt-6 flex items-center justify-between border-t border-white/10 pt-4 text-sm text-white/60">
        <span>{createdAt}</span>
        <span>{reaction.auraDelta > 0 ? '+' : ''}{reaction.auraDelta} aura</span>
      </div>
    </div>
  );
}
