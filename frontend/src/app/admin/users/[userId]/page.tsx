"use client";

import Link from "next/link";
import { use, useState } from "react";
import { AccountStatusBadge } from "@/components/admin/AccountStatusBadge";
import { ConfirmDialog } from "@/components/admin/ConfirmDialog";
import { StatCard } from "@/components/admin/StatCard";
import { useAdminToast } from "@/components/admin/AdminToast";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  useAdminUserDetail,
  useAdminUserRole,
  useAdminUserStatus,
} from "@/hooks/admin/useAdmin";
import { useSession } from "@/hooks/useSession";
import { ACCOUNT_STATUSES } from "@/lib/admin/types";
import { getApiErrorMessage } from "@/lib/api";
import { cn } from "@/lib/utils";

type PendingAction =
  | { kind: "grant" }
  | { kind: "revoke" }
  | { kind: "status"; status: string }
  | null;

export default function AdminUserDetailPage({
  params,
}: {
  params: Promise<{ userId: string }>;
}) {
  const { userId } = use(params);
  const { data: user, isLoading, isError, refetch } = useAdminUserDetail(userId);
  const { data: session } = useSession();
  const roleMutation = useAdminUserRole();
  const statusMutation = useAdminUserStatus();
  const { showToast } = useAdminToast();

  const [pending, setPending] = useState<PendingAction>(null);

  const isSelf = session?.userId === userId;

  const runPendingAction = async () => {
    if (!pending) return;

    try {
      if (pending.kind === "status") {
        const result = await statusMutation.mutateAsync({
          userId,
          status: pending.status,
        });
        showToast(messageOf(result) ?? `Account status set to ${pending.status}.`);
      } else {
        const grant = pending.kind === "grant";
        const result = await roleMutation.mutateAsync({ userId, grant });
        showToast(messageOf(result) ?? (grant ? "Admin role granted." : "Admin role removed."));
      }
      refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    } finally {
      setPending(null);
    }
  };

  if (isLoading) {
    return <Skeleton className="h-96 w-full" />;
  }

  if (isError || !user) {
    return (
      <div className="space-y-4">
        <p className="text-destructive">Failed to load this user.</p>
        <Link href="/admin/users" className={cn(buttonVariants({ variant: "outline" }))}>
          Back to users
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <Link
          href="/admin/users"
          className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
        >
          ← Back
        </Link>
        <div className="mt-3 flex flex-wrap items-center gap-3">
          <h2 className="text-xl font-semibold">{user.displayName || user.email}</h2>
          <AccountStatusBadge status={user.accountStatus} />
          {user.isPlatformAdmin ? <Badge>Platform admin</Badge> : null}
          {isSelf ? <Badge variant="outline">You</Badge> : null}
        </div>
        <p className="mt-1 break-anywhere text-sm text-zinc-500">{user.email}</p>
        <p className="mt-1 font-mono text-xs text-zinc-600">{user.id}</p>
      </div>

      {user.identitySource === "database" ? (
        <div className="rounded-lg border border-amber-900/60 bg-amber-950/30 p-4 text-sm text-amber-200">
          Supabase service-role key is not configured, so sign-in history and login providers
          cannot be shown.
        </div>
      ) : null}

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Predictions" value={user.activity.predictionCount} />
        <StatCard label="Leagues" value={user.activity.leagueCount} />
        <StatCard label="AI generations" value={user.activity.generatedContentCount} />
        <StatCard
          label="Last prediction"
          value={
            user.activity.lastPredictionAt
              ? new Date(user.activity.lastPredictionAt).toLocaleDateString()
              : "—"
          }
        />
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <DetailCard title="Application profile">
          <DetailRow label="Display name" value={user.displayName} />
          <DetailRow label="Country" value={user.countryCode} />
          <DetailRow label="Account status" value={user.accountStatus} />
          <DetailRow label="Platform admin" value={user.isPlatformAdmin ? "Yes" : "No"} />
          <DetailRow
            label="Terms accepted"
            value={user.termsAcceptedAt ? new Date(user.termsAcceptedAt).toLocaleString() : null}
          />
          <DetailRow label="Created" value={new Date(user.createdAt).toLocaleString()} />
        </DetailCard>

        <DetailCard title="Supabase identity">
          {user.identity ? (
            <>
              <DetailRow
                label="Created"
                value={
                  user.identity.createdAt
                    ? new Date(user.identity.createdAt).toLocaleString()
                    : null
                }
              />
              <DetailRow
                label="Last sign-in"
                value={
                  user.identity.lastSignInAt
                    ? new Date(user.identity.lastSignInAt).toLocaleString()
                    : null
                }
              />
              <DetailRow
                label="Email confirmed"
                value={
                  user.identity.emailConfirmedAt
                    ? new Date(user.identity.emailConfirmedAt).toLocaleString()
                    : "Not confirmed"
                }
              />
              <DetailRow
                label="Login methods"
                value={user.identity.providers.join(", ") || null}
              />
              <DetailRow label="Banned in Supabase" value={user.identity.isBanned ? "Yes" : "No"} />
            </>
          ) : (
            <p className="text-sm text-zinc-500">
              No Supabase identity record is available for this account.
            </p>
          )}
        </DetailCard>
      </div>

      <DetailCard title="League memberships">
        {user.leagues.length === 0 ? (
          <p className="text-sm text-zinc-500">Not a member of any league.</p>
        ) : (
          <ul className="space-y-2">
            {user.leagues.map((league) => (
              <li
                key={league.leagueId}
                className="flex flex-wrap items-center justify-between gap-2 border-b border-zinc-900 py-2 text-sm last:border-0"
              >
                <span className="text-zinc-200">{league.name}</span>
                <span className="flex items-center gap-2 text-xs text-zinc-500">
                  {league.kind}
                  {league.isLeagueAdmin ? <Badge variant="outline">League admin</Badge> : null}
                </span>
              </li>
            ))}
          </ul>
        )}
      </DetailCard>

      <DetailCard title="Management">
        <p className="mb-4 text-sm text-zinc-500">
          Role and status changes take effect on the account&apos;s next request and are written to
          the admin audit log. Account deletion is not available here; suspend or ban instead.
        </p>

        {user.isAllowlisted ? (
          <p className="mb-4 rounded-md border border-amber-900/60 bg-amber-950/30 p-3 text-xs text-amber-200">
            This account is in the Admin allowlist configuration. Removing the role will not stick
            until it is also removed from <code>Admin__AllowedEmails</code> or{" "}
            <code>Admin__AllowedUserIds</code>.
          </p>
        ) : null}

        <div className="flex flex-wrap gap-2">
          {user.isPlatformAdmin ? (
            <Button
              size="sm"
              variant="destructive"
              disabled={isSelf}
              onClick={() => setPending({ kind: "revoke" })}
            >
              Remove admin role
            </Button>
          ) : (
            <Button size="sm" onClick={() => setPending({ kind: "grant" })}>
              Grant admin role
            </Button>
          )}

          {ACCOUNT_STATUSES.filter((status) => status !== user.accountStatus).map((status) => (
            <Button
              key={status}
              size="sm"
              variant="outline"
              disabled={isSelf || (user.isPlatformAdmin && (status === "Suspended" || status === "Banned"))}
              onClick={() => setPending({ kind: "status", status })}
            >
              Set {status}
            </Button>
          ))}
        </div>

        {isSelf ? (
          <p className="mt-3 text-xs text-zinc-500">
            You cannot change your own role or status.
          </p>
        ) : null}
      </DetailCard>

      <ConfirmDialog
        open={pending !== null}
        onOpenChange={(open) => {
          if (!open) setPending(null);
        }}
        title={confirmTitle(pending)}
        description={confirmDescription(pending, user.displayName || user.email)}
        confirmLabel={pending?.kind === "grant" ? "Grant role" : "Confirm"}
        destructive={pending?.kind === "revoke" || pending?.kind === "status"}
        onConfirm={runPendingAction}
      />
    </div>
  );
}

function messageOf(result: unknown): string | null {
  if (result && typeof result === "object" && "message" in result) {
    const message = (result as { message?: unknown }).message;
    return typeof message === "string" && message.length > 0 ? message : null;
  }
  return null;
}

function confirmTitle(pending: PendingAction) {
  if (!pending) return "";
  if (pending.kind === "grant") return "Grant admin role";
  if (pending.kind === "revoke") return "Remove admin role";
  return `Set account status to ${pending.status}`;
}

function confirmDescription(pending: PendingAction, name: string) {
  if (!pending) return "";
  if (pending.kind === "grant") {
    return `${name} will gain full access to the admin console, including user management and job execution.`;
  }
  if (pending.kind === "revoke") {
    return `${name} will lose access to the admin console.`;
  }
  if (pending.status === "Banned") {
    return `${name} will be marked as banned. Their existing predictions and league memberships are kept.`;
  }
  if (pending.status === "Suspended") {
    return `${name} will be marked as suspended. This is reversible.`;
  }
  return `${name} will be marked as ${pending.status}.`;
}

function DetailCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-zinc-800 p-4">
      <h3 className="mb-3 text-sm font-semibold text-zinc-300">{title}</h3>
      {children}
    </div>
  );
}

function DetailRow({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div className="flex flex-col gap-1 border-b border-zinc-900 py-2 text-sm last:border-0 sm:flex-row sm:justify-between sm:gap-4">
      <span className="text-zinc-500">{label}</span>
      <span className="break-all text-zinc-200 sm:text-right">{value ?? "—"}</span>
    </div>
  );
}
