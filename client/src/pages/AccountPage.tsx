import { useState } from "react";
import { useAddresses, useAddressMutations, useMarkNotificationsRead, useNotifications, useUpdateProfile } from "../api/hooks";
import { useAuth } from "../stores/auth";
import { useToasts } from "../stores/toast";
import { ApiError } from "../api/client";
import { EmptyState, Spinner } from "../components/ui";

const inputCls = "w-full rounded-xl border border-zinc-200 bg-transparent px-4 py-2.5 text-sm outline-none transition focus:border-brand-400 dark:border-zinc-700";

export function AccountPage() {
  const user = useAuth((s) => s.user);
  const update = useUpdateProfile();
  const toast = useToasts((s) => s.push);
  const { data: addresses } = useAddresses();
  const { remove } = useAddressMutations();

  const [fullName, setFullName] = useState(user?.fullName ?? "");
  const [phone, setPhone] = useState(user?.phone ?? "");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");

  if (!user) return <Spinner />;

  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await update.mutateAsync({
        fullName,
        phone,
        currentPassword: currentPassword || undefined,
        newPassword: newPassword || undefined,
      });
      toast("Profile updated ✓");
      setCurrentPassword("");
      setNewPassword("");
    } catch (err) {
      toast(err instanceof ApiError ? err.message : "Update failed", "error");
    }
  };

  return (
    <div className="mx-auto max-w-2xl space-y-10 pb-10">
      <section>
        <h1 className="mb-1 text-2xl font-extrabold">My account</h1>
        <p className="text-sm text-zinc-500">
          {user.email} {user.emailVerified
            ? <span className="text-emerald-600">· verified ✓</span>
            : <span className="text-amber-600">· not verified</span>}
        </p>

        <form onSubmit={save} className="mt-5 space-y-3">
          <input value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Full name" className={inputCls} />
          <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="Phone" className={inputCls} />
          <div className="grid gap-3 sm:grid-cols-2">
            <input type="password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} placeholder="Current password" className={inputCls} />
            <input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} placeholder="New password (optional)" className={inputCls} />
          </div>
          <button type="submit" disabled={update.isPending}
            className="rounded-full bg-gradient-to-r from-brand-500 to-accent-500 px-7 py-2.5 text-sm font-bold text-white disabled:opacity-40">
            {update.isPending ? "Saving…" : "Save changes"}
          </button>
        </form>
      </section>

      <section>
        <h2 className="mb-3 text-lg font-extrabold">Saved addresses</h2>
        {addresses && addresses.length > 0 ? (
          <div className="space-y-2">
            {addresses.map((a) => (
              <div key={a.id} className="flex items-center justify-between rounded-2xl border border-zinc-200 p-4 text-sm dark:border-zinc-800">
                <div>
                  <p className="font-bold">{a.label} {a.isDefault && <span className="text-xs text-brand-500">· default</span>}</p>
                  <p className="text-zinc-500">{a.fullName} · {a.line1}, {a.city}, {a.county} · {a.phone}</p>
                </div>
                <button onClick={() => remove.mutate(a.id)} className="text-zinc-400 hover:text-rose-500" aria-label="Delete">✕</button>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-zinc-500">No saved addresses — you can add one during checkout.</p>
        )}
      </section>
    </div>
  );
}

export function NotificationsPage() {
  const { data, isLoading } = useNotifications();
  const markRead = useMarkNotificationsRead();

  if (isLoading) return <Spinner />;
  if (!data || data.items.length === 0)
    return <EmptyState icon="🔔" title="No notifications" subtitle="Order updates and offers will show up here." />;

  return (
    <div className="mx-auto max-w-2xl pb-10">
      <div className="mb-5 flex items-center justify-between">
        <h1 className="text-2xl font-extrabold">Notifications</h1>
        <button onClick={() => markRead.mutate(null)} className="text-sm font-semibold text-brand-500">
          Mark all read
        </button>
      </div>
      <div className="space-y-2">
        {data.items.map((n) => (
          <div key={n.id}
            className={`rounded-2xl border p-4 ${n.isRead ? "border-zinc-200 opacity-70 dark:border-zinc-800" : "border-brand-200 bg-brand-50/40 dark:border-brand-500/30 dark:bg-brand-500/5"}`}>
            <p className="font-semibold">{n.title}</p>
            <p className="text-sm text-zinc-600 dark:text-zinc-400">{n.body}</p>
            <p className="mt-1 text-xs text-zinc-400">{new Date(n.createdAt).toLocaleString()}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
