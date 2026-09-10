import { BanterCard } from "@/components/feed/BanterCard";
import { CommunityReceiptCard } from "@/components/feed/CommunityReceiptCard";
import { FallbackFeedCard } from "@/components/feed/FallbackFeedCard";
import { GifReactionCard } from "@/components/feed/GifReactionCard";
import { LeaderboardCard } from "@/components/feed/LeaderboardCard";
import { MatchEventCard } from "@/components/feed/MatchEventCard";
import { MemeCard } from "@/components/feed/MemeCard";
import { NewsCard } from "@/components/feed/NewsCard";
import { PredictionHighlightCard } from "@/components/feed/PredictionHighlightCard";
import { PunditQuoteCard } from "@/components/feed/PunditQuoteCard";
import { PunditReceiptCard } from "@/components/feed/PunditReceiptCard";
import { StudioStoryCard } from "@/components/feed/StudioStoryCard";
import { TrendingTakeCard } from "@/components/feed/TrendingTakeCard";
import { UserPunditCompareCard } from "@/components/feed/UserPunditCompareCard";
import type { FeedItem } from "@/lib/types";

/** Discriminated renderer: one card per type, fallback for anything unexpected. */
export function FeedCard({ item }: { item: FeedItem }) {
  switch (item.type) {
    case "banter":
      return <BanterCard item={item} />;
    case "meme":
      return <MemeCard item={item} />;
    case "news":
      return <NewsCard item={item} />;
    case "leaderboard":
      return <LeaderboardCard item={item} />;
    case "prediction_highlight":
      return <PredictionHighlightCard item={item} />;
    case "pundit_quote":
      return <PunditQuoteCard item={item} />;
    case "gif_reaction":
      return <GifReactionCard item={item} />;
    case "pundit_receipt":
      return <PunditReceiptCard item={item} />;
    case "user_pundit_compare":
      return <UserPunditCompareCard item={item} />;
    case "community_receipt":
      return <CommunityReceiptCard item={item} />;
    case "trending_take":
      return <TrendingTakeCard item={item} />;
    case "match_event":
      return <MatchEventCard item={item} />;
    case "studio_story":
      return <StudioStoryCard item={item} />;
    default:
      return <FallbackFeedCard item={item} />;
  }
}
