import { cn } from '@/lib/utils';

export function BanterLine({
  emoji = '⚽',
  text,
  className,
}: {
  emoji?: string;
  text: string;
  className?: string;
}) {
  return (
    <div
      className={cn(
        'banter-line text-foreground',
        className
      )}
    >
      <span className="mr-2" aria-hidden>{emoji}</span>
      {text}
    </div>
  );
}
