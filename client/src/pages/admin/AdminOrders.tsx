import { useState } from "react";
import { useAdminOrders, useUpdateOrderStatus } from "../../api/adminHooks";
import { useToasts } from "../../stores/toast";
import { ApiError } from "../../api/client";
import { Pagination, Price, Spinner, StatusBadge } from "../../components/ui";

const STATUSES = ["", "PendingPayment", "Paid", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded"];
const NEXT_ACTIONS: Record<string, string[]> = {
  Paid: ["Processing", "Shipped"],
  Processing: ["Shipped"],
  Shipped: ["Delivered"],
};

export default function AdminOrders() {
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const { data, isLoading } = useAdminOrders(status, page);
  const update = useUpdateOrderStatus();
  const toast = useToasts((s) => s.push);
  const [tracking, setTracking] = useState<Record<string, string>>({});

  const move = async (orderId: string, next: string) => {
    try {
      await update.mutateAsync({ orderId, status: next, trackingNumber: tracking[orderId] || null });
      toast(`Order → ${next}`);
    } catch (e) {
      toast(e instanceof ApiError ? e.message : "Update failed", "error");
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-extrabold">Orders</h1>
        <select value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}
          className="rounded-lg border border-zinc-200 bg-white px-3 py-1.5 text-sm dark:border-zinc-700 dark:bg-zinc-900">
          {STATUSES.map((s) => <option key={s} value={s}>{s === "" ? "All statuses" : s}</option>)}
        </select>
      </div>

      {isLoading || !data ? <Spinner /> : (
        <div className="overflow-x-auto rounded-2xl border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900">
          <table className="w-full min-w-[720px] text-sm">
            <thead>
              <tr className="border-b border-zinc-100 text-left text-xs uppercase tracking-wide text-zinc-400 dark:border-zinc-800">
                <th className="px-4 py-3">Order</th>
                <th className="px-4 py-3">Customer</th>
                <th className="px-4 py-3">Items</th>
                <th className="px-4 py-3">Total</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((o) => (
                <tr key={o.id} className="border-b border-zinc-50 last:border-0 dark:border-zinc-800/50">
                  <td className="px-4 py-3">
                    <p className="font-semibold">{o.orderNumber}</p>
                    <p className="text-xs text-zinc-400">{new Date(o.placedAt).toLocaleString()}</p>
                  </td>
                  <td className="px-4 py-3">
                    <p>{o.shippingAddress.fullName}</p>
                    <p className="text-xs text-zinc-400">{o.shippingAddress.city}</p>
                  </td>
                  <td className="px-4 py-3">{o.items.reduce((n, i) => n + i.quantity, 0)}</td>
                  <td className="px-4 py-3"><Price value={o.total} /></td>
                  <td className="px-4 py-3"><StatusBadge status={o.status} /></td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap items-center gap-2">
                      {(NEXT_ACTIONS[o.status] ?? []).map((next) => (
                        <button key={next} onClick={() => move(o.id, next)} disabled={update.isPending}
                          className="rounded-full bg-brand-500 px-3 py-1 text-xs font-bold text-white disabled:opacity-40">
                          → {next}
                        </button>
                      ))}
                      {o.status === "Paid" || o.status === "Processing" ? (
                        <input placeholder="Tracking #" value={tracking[o.id] ?? ""}
                          onChange={(e) => setTracking({ ...tracking, [o.id]: e.target.value })}
                          className="w-24 rounded-lg border border-zinc-200 bg-transparent px-2 py-1 text-xs dark:border-zinc-700" />
                      ) : o.trackingNumber && <span className="text-xs text-zinc-400">🚚 {o.trackingNumber}</span>}
                    </div>
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
