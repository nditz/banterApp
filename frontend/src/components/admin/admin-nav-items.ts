export const adminNavItems: Array<{
  href: string;
  label: string;
  exact?: boolean;
}> = [
  { href: "/admin", label: "Overview", exact: true },
  { href: "/admin/jobs", label: "Jobs" },
  { href: "/admin/errors", label: "Errors" },
  { href: "/admin/sources", label: "Sources" },
  { href: "/admin/source-items", label: "Source Items" },
  { href: "/admin/review", label: "Review" },
  { href: "/admin/stats", label: "Stats" },
  { href: "/admin/health", label: "Health" },
  { href: "/admin/launch-checklist", label: "Launch Checklist" },
] ;

export function isAdminNavActive(pathname: string, href: string, exact?: boolean) {
  return exact ? pathname === href : pathname.startsWith(href);
}
