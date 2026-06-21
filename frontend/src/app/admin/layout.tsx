import type { Metadata } from "next";
import { AdminShell } from "@/components/admin/AdminShell";
import { AdminToastProvider } from "@/components/admin/AdminToast";

export const metadata: Metadata = {
  robots: { index: false, follow: false },
};

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <AdminToastProvider>
      <AdminShell>{children}</AdminShell>
    </AdminToastProvider>
  );
}
