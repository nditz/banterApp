/** Append signedIn=1 so the app can show a “you're logged in” confirmation. */
export function withSignedInQuery(path: string): string {
  const [pathnameAndSearch, hash = ""] = path.split("#");
  const [pathname, search = ""] = pathnameAndSearch.split("?");
  const params = new URLSearchParams(search);
  params.set("signedIn", "1");
  const query = params.toString();
  return `${pathname}?${query}${hash ? `#${hash}` : ""}`;
}

export function markJustSignedIn(): void {
  try {
    sessionStorage.setItem("banter_just_signed_in", "1");
  } catch {
    /* private mode */
  }
}

