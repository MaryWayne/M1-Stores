import { useState } from "react";
import { useCoupons, useCustomers, useDeleteCoupon, useSaveCoupon, useSetCustomerActive } from "../../api/adminHooks";
import { useToasts } from "../../stores/toast";
import { ApiError } from "../../api/client";
import { Pagination, Price, Spinner } from "../../components/ui";

const inputCls = "w-full rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-700";

export function AdminCustomers() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const { data, isLoading } = useCustomers(search, page);
  const setActive = useSetCustomerActive();
  const toast = useToasts((s) => s.push);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-extrabold">Customers</h1>
        <input value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          placeholder="Search name or email…" className={`${inputCls} w-64`} />
      </div>

      {isLoading || !data ? <Spinner /> : (
        <div className="overflow-x-auto rounded-2xl border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900">
          <table className="w-full min-w-[640px] text-sm">
            <thead>
              <tr className="border-b border-zinc-100 text-left text-xs uppercase tracking-wide text-zinc-400 dark:border-zinc-800">
                <th className="px-4 py-3">Customer</th>
                <th className="px-4 py-3">Orders</th>
                <th className="px-4 py-3">Total spent</th>
                <th className="px-4 py-3">Joined</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((c) => (
                <tr key={c.id} className="border-b border-zinc-50 last:border-0 dark:border-zinc-800/50">
                  <td className="px-4 py-3">
                    <p className="font-semibold">{c.fullName}</p>
                    <p className="text-xs text-zinc-400">{c.email} {c.emailVerified && "✓"}</p>
                  </td>
                  <td className="px-4 py-3">{c.orders}</td>
                  <td className="px-4 py-3"><Price value={c.totalSpent} /></td>
                  <td className="px-4 py-3 text-zinc-500">{new Date(c.joinedAt).toLocaleDateString()}</td>
                  <td className="px-4 py-3">
                    <span className={c.isDeactivated ? "font-semibold text-rose-500" : "text-emerald-600"}>
                      {c.isDeactivated ? "Deactivated" : "Active"}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={async () => {
                        await setActive.mutateAsync({ userId: c.id, active: c.isDeactivated });
                        toast(c.isDeactivated ? "Customer reactivated" : "Customer deactivated");
                      }}
                      className={`rounded-full border px-3 py-1 text-xs font-semibold ${c.isDeactivated ? "border-emerald-300 text-emerald-600" : "border-rose-200 text-rose-500 dark:border-rose-500/40"}`}>
                      {c.isDeactivated ? "Reactivate" : "Deactivate"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="px-4 pb-4">
            <Pagination page={data.page} totalPages={data.totalPages} onPage={setPage} />
          </div>
        </div>
      )}
    </div>
  );
}

export function AdminCoupons() {
  const { data: coupons, isLoading } = useCoupons();
  const save = useSaveCoupon();
  const remove = useDeleteCoupon();
  const toast = useToasts((s) => s.push);
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState({ code: "", type: 0, value: 10, minOrderTotal: "", maxUses: 100 });

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await save.mutateAsync({
        body: {
          code: form.code, type: form.type, value: form.value,
          minOrderTotal: form.minOrderTotal ? Number(form.minOrderTotal) : null,
          maxUses: form.maxUses, expiresAt: null, isActive: true,
        },
      });
      toast("Coupon created 🎟️");
      setCreating(false);
      setForm({ code: "", type: 0, value: 10, minOrderTotal: "", maxUses: 100 });
    } catch (err) {
      toast(err instanceof ApiError ? err.message : "Save failed", "error");
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-extrabold">Coupons</h1>
        <button onClick={() => setCreating(!creating)}
          className="rounded-full bg-gradient-to-r from-brand-500 to-accent-500 px-5 py-2 text-sm font-bold text-white">
          + New coupon
        </button>
      </div>

      {creating && (
        <form onSubmit={submit} className="grid gap-3 rounded-2xl border border-brand-200 bg-white p-5 sm:grid-cols-5 dark:border-brand-500/30 dark:bg-zinc-900">
          <input required value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value.toUpperCase() })} placeholder="CODE" className={inputCls} />
          <select value={form.type} onChange={(e) => setForm({ ...form, type: Number(e.target.value) })} className={inputCls}>
            <option value={0}>Percent off</option>
            <option value={1}>Fixed KES off</option>
          </select>
          <input required type="number" min="1" value={form.value} onChange={(e) => setForm({ ...form, value: Number(e.target.value) })} placeholder="Value" className={inputCls} />
          <input type="number" value={form.minOrderTotal} onChange={(e) => setForm({ ...form, minOrderTotal: e.target.value })} placeholder="Min order (opt)" className={inputCls} />
          <div className="flex gap-2">
            <input required type="number" min="1" value={form.maxUses} onChange={(e) => setForm({ ...form, maxUses: Number(e.target.value) })} placeholder="Max uses" className={inputCls} />
            <button type="submit" disabled={save.isPending} className="shrink-0 rounded-full bg-brand-500 px-4 py-2 text-sm font-bold text-white disabled:opacity-40">Save</button>
          </div>
        </form>
      )}

      {isLoading || !coupons ? <Spinner /> : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {coupons.map((c) => (
            <div key={c.id} className={`rounded-2xl border p-4 ${c.isActive ? "border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900" : "border-zinc-100 bg-zinc-50 opacity-60 dark:border-zinc-800 dark:bg-zinc-900"}`}>
              <div className="flex items-center justify-between">
                <span className="font-mono text-lg font-extrabold tracking-wide">{c.code}</span>
                {c.isActive ? (
                  <button onClick={async () => { await remove.mutateAsync(c.id); toast("Coupon deactivated"); }}
                    className="text-xs font-semibold text-rose-500">Deactivate</button>
                ) : <span className="text-xs text-zinc-400">Inactive</span>}
              </div>
              <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
                {c.type === "Percent" ? `${c.value}% off` : `KES ${c.value.toLocaleString()} off`}
                {c.minOrderTotal && ` · min KES ${c.minOrderTotal.toLocaleString()}`}
              </p>
              <p className="mt-2 text-xs text-zinc-400">Used {c.usedCount} / {c.maxUses}</p>
              <div className="mt-1.5 h-1.5 overflow-hidden rounded-full bg-zinc-100 dark:bg-zinc-800">
                <div className="h-full rounded-full bg-brand-500" style={{ width: `${Math.min(100, (c.usedCount / c.maxUses) * 100)}%` }} />
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
