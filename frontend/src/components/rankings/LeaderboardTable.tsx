import { Skeleton } from "@/components/ui/skeleton";
import { UserAvatar } from "@/components/ui/UserAvatar";
import { getPunditAvatarUrl, formatPunditSubtitle } from "@/lib/pundits";
import type { LeaderboardEntry } from "@/lib/types";
import { cn } from "@/lib/utils";

interface LeaderboardTableProps {
  entries: LeaderboardEntry[];
  isLoading?: boolean;
  /** The current user's row, pinned below the table when outside the top list. */
  me?: LeaderboardEntry | null;
  /** Total number of ranked players, for "Rank X of Y" context. */
  totalPlayers?: number;
}

function rankStyle(rank: number): string {
  if (rank === 1) return "rank-gold";
  if (rank === 2) return "rank-silver";
  if (rank === 3) return "rank-bronze";
  return "";
}

function formatCount(value: number): string {
  return new Intl.NumberFormat("en-GB").format(value);
}

function PlayerRow({ entry }: { entry: LeaderboardEntry }) {
  const displayName = entry.displayName?.trim() || "Player";
  const isMe = entry.isCurrentUser || displayName === "You";
  const avatarUrl =
    entry.avatarUrl ??
    (entry.isPundit ? getPunditAvatarUrl(entry.userId, displayName) : undefined);

  return (
    <tr
      className={cn(
        "border-b border-border/60 last:border-0",
        rankStyle(entry.rank),
        isMe && "bg-pitch/10 font-medium ring-1 ring-inset ring-pitch/30"
      )}
    >
      <td className="px-2 py-2 tabular-nums text-muted-foreground">
        {entry.rank}
      </td>
      <td className="px-2 py-2">
        <div className="flex items-center gap-2">
          <UserAvatar
            userId={entry.userId}
            displayName={displayName}
            avatarUrl={avatarUrl}
            size={24}
            highlight={isMe}
          />
          <div className="min-w-0">
            <span className="block truncate text-sm">{displayName}</span>
            {(entry.parodyCue || entry.organization) && (
              <span className="block text-[10px] leading-snug text-flare/90">
                {formatPunditSubtitle(entry) ?? entry.organization}
              </span>
            )}
            {entry.isPundit && entry.organization && entry.parodyCue && (
              <span className="text-[10px] text-muted-foreground">{entry.organization}</span>
            )}
            {!entry.parodyCue && entry.organization && !entry.isPundit && (
              <span className="text-[10px] text-muted-foreground">{entry.organization}</span>
            )}
          </div>
        </div>
      </td>
      <td className="hidden px-2 py-2 text-right tabular-nums text-muted-foreground sm:table-cell">
        {entry.correctPredictions ?? "—"}
        {entry.totalPredictions ? ` / ${entry.totalPredictions}` : ""}
      </td>
      <td className="px-2 py-2 text-right text-sm font-semibold tabular-nums">
        {entry.points}
      </td>
    </tr>
  );
}

export function LeaderboardTable({
  entries,
  isLoading,
  me,
  totalPlayers,
}: LeaderboardTableProps) {
  if (isLoading) {
    return (
      <div className="space-y-1.5" aria-busy="true">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-9 w-full rounded-md" />
        ))}
      </div>
    );
  }

  if (entries.length === 0) {
    return (
      <p className="py-8 text-center text-sm text-muted-foreground">
        No receipts on the board yet. Time to put your ball knowledge on record.
      </p>
    );
  }

  const meInList = me ? entries.some((e) => e.rank === me.rank) : false;
  const showPinnedMe = Boolean(me && !meInList);
  const total = totalPlayers ?? entries.length;
  const topPercent =
    me && total > 0 ? Math.max(1, Math.ceil((me.rank / total) * 100)) : null;

  return (
    <div>
      <div className="overflow-x-auto rounded-xl border border-border glass-card">
        <table className="w-full min-w-[280px] text-xs">
          <thead>
            <tr className="border-b border-border bg-muted/40">
              <th scope="col" className="px-2.5 py-2 text-left text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                #
              </th>
              <th scope="col" className="px-2.5 py-2 text-left text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                Player
              </th>
              <th scope="col" className="hidden px-2.5 py-2 text-right text-[10px] font-bold uppercase tracking-wider text-muted-foreground sm:table-cell">
                Correct
              </th>
              <th scope="col" className="px-2.5 py-2 text-right text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                Pts
              </th>
            </tr>
          </thead>
          <tbody>
            {entries.map((entry, index) => (
              <PlayerRow key={entry.userId ?? `rank-${index}`} entry={entry} />
            ))}

            {/* FPL-style pinned row: your position relative to the top list */}
            {showPinnedMe && me && (
              <>
                <tr aria-hidden>
                  <td
                    colSpan={4}
                    className="border-b border-dashed border-border/80 px-2 py-1 text-center text-[10px] tracking-[0.3em] text-muted-foreground"
                  >
                    •••
                  </td>
                </tr>
                <PlayerRow entry={me} />
              </>
            )}
          </tbody>
        </table>
      </div>

      <p className="mt-2 text-center text-[11px] text-muted-foreground">
        {me ? (
          <>
            Your rank:{" "}
            <span className="font-semibold text-foreground">
              {formatCount(me.rank)}
            </span>{" "}
            of {formatCount(total)} players
            {topPercent !== null && topPercent <= 50 && (
              <span className="ml-1 rounded-full bg-pitch/15 px-1.5 py-0.5 font-semibold text-pitch">
                Top {topPercent}%
              </span>
            )}
          </>
        ) : (
          <>{formatCount(total)} players ranked</>
        )}
      </p>
    </div>
  );
}
