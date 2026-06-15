"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useEffect, useState, type ReactNode } from "react";
import { getOrCreateAnonymousUser } from "@/lib/anonymous";

function AnonymousUserInit() {
  useEffect(() => {
    getOrCreateAnonymousUser();
  }, []);
  return null;
}

export function QueryProvider({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            retry: 1,
            refetchOnWindowFocus: false,
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <AnonymousUserInit />
      {children}
    </QueryClientProvider>
  );
}
