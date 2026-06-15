"use client";

import { useSession } from "@/hooks/useSession";

export function useNeedsTerms() {
  const { data: session, isLoading, isError } = useSession();
  const needsTerms = !isLoading && (isError || !session?.termsAccepted);
  return { needsTerms, session, isLoading, isError };
}
