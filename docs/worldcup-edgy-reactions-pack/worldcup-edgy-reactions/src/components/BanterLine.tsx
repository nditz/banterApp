export function BanterLine({ emoji = '⚽', text }: { emoji?: string; text: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.04] px-4 py-3 text-sm text-white/85">
      <span className="mr-2">{emoji}</span>{text}
    </div>
  );
}
