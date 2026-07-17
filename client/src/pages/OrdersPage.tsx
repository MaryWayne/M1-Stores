import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useCancelOrder, useOrder, useOrders } from "../api/hooks";
import { useToasts } from "../stores/toast";
import { EmptyState, Pagination, Price, Spinner, StatusBadge } from "../components/ui";

export function OrdersPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useOrders(page);

  if (isLoading) return <Spinner />;
  if (!data || data.items.length === 0)
    return <EmptyState icon="📦" title="No orders yet" subtitle="Your orders will appear here after checkout."
      action={<Link to="/shop" className="rounded-full bg-brand-500 px-6 py-2.5 text-sm font-bold text-white">Start shopping</Link>} />;

  return (
    <div className="mx-auto max-w-3xl pb-10">
      <h1 className="mb-6 text-2xl font-extrabold">My orders</h1>
      <div className="space-y-3">
        {data.items.map((o) => (
          <Link key={o.id} to={`/orders/${o.orderNumber}`}
            className="flex items-center gap-4 rounded-2xl border border-zinc-200 p-4 transition hover:border-brand-300 hover:shadow-sm dark:border-zinc-800">
            <div className="h-16 w-16 shrink-0 overflow-hidden rounded-xl bg-zinc-100 dark:bg-zinc-800">
              {o.firstImageUrl && <img src={o.firstImageUrl} alt="" className="h-full w-full object-cover" />}
            </div>
            <div className="min-w-0 flex-1">
              <p className="font-bold">{o.orderNumber}</p>
              <p className="text-sm text-zinc-500">
                {new Date(o.placedAt).toLocaleDateString()} · {o.itemCount} item{o.itemCount === 1 ? "" : "s"}
              </p>
            </div>
            <div className="flex flex-col items-end gap-1.5">
              <StatusBadge status={o.status} />
              <Price value={o.total} className="text-sm" />
            </div>
          </Link>
        ))}
      </div>
      <Pagination page={data.page} totalPages={data.totalPages} onPage={setPage} />
    </div>
  );
}

export function OrderDetailPage() {
  const { orderNumber } = useParams();
  const { data: order, isLoading } = useOrder(orderNumber);
  const cancel = useCancelOrder();
  const toast = useToasts((s) => s.push);

  if (isLoading || !order) return <Spinner />;

  const apiBase = (import.meta.env.VITE_API_URL as string | undefined) ?? "";

  return (
    <div className="mx-auto max-w-3xl space-y-8 pb-10">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <Link to="/orders" className="text-sm text-zinc-500 hover:text-brand-500">← My orders</Link>
          <h1 className="text-2xl font-extrabold">{order.orderNumber}</h1>
          <p className="text-sm text-zinc-500">Placed {new Date(order.placedAt).toLocaleString()}</p>
        </div>
        <StatusBadge status={order.status} />
      </div>

      {/* Timeline */}
      <div className="flex items-center gap-0 overflow-x-auto rounded-2xl border border-zinc-200 p-5 dark:border-zinc-800">
        {order.timeline.filter((t) => ["Placed", "Paid", "Shipped", "Delivered"].includes(t.status)).map((t, i, arr) => (
          <div key={t.status} className="flex flex-1 items-center">
            <div className="flex min-w-16 flex-col items-center gap-1">
              <span className={`flex h-8 w-8 items-center justify-center rounded-full text-sm font-bold ${t.at ? "bg-emerald-500 text-white" : "bg-zinc-200 text-zinc-400 dark:bg-zinc-800"}`}>
                {t.at ? "✓" : i + 1}
              </span>
              <span className="text-xs font-semibold">{t.status}</span>
              {t.at && <span className="text-[10px] text-zinc-400">{new Date(t.at).toLocaleDateString()}</span>}
            </div>
            {i < arr.length - 1 && <div className={`h-0.5 flex-1 ${arr[i + 1].at ? "bg-emerald-500" : "bg-zinc-200 dark:bg-zinc-800"}`} />}
          </div>
        ))}
      </div>
      {order.trackingNumber && (
        <p className="text-sm">🚚 Tracking number: <span className="font-bold">{order.trackingNumber}</span></p>
      )}

      {/* Items */}
      <div className="space-y-3">
        {order.items.map((i, idx) => (
          <div key={idx} className="flex items-center gap-4 rounded-2xl border border-zinc-200 p-4 dark:border-zinc-800">
            <div className="h-16 w-16 shrink-0 overflow-hidden rounded-xl bg-zinc-100 dark:bg-zinc-800">
              {i.imageUrl && <img src={i.imageUrl} alt="" className="h-full w-full object-cover" />}
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate font-semibold">{i.productName}</p>
              <p className="text-sm text-zinc-500">{i.variantLabel} · {i.quantity} × <Price value={i.unitPrice} className="font-medium" /></p>
            </div>
            <Price value={i.lineTotal} />
          </div>
        ))}
      </div>

      {/* Totals + address */}
      <div className="grid gap-6 sm:grid-cols-2">
        <div className="rounded-2xl bg-zinc-50 p-5 text-sm dark:bg-zinc-900">
          <h3 className="mb-2 font-bold">Delivery address</h3>
          <p>{order.shippingAddress.fullName} · {order.shippingAddress.phone}</p>
          <p className="text-zinc-500">{order.shippingAddress.line1}, {order.shippingAddress.city}, {order.shippingAddress.county}</p>
          <p className="mt-3 text-zinc-500">Paid via <span className="font-semibold text-zinc-700 dark:text-zinc-300">{order.paymentProvider}</span> · {order.paymentStatus}</p>
        </div>
        <div className="rounded-2xl bg-zinc-50 p-5 text-sm dark:bg-zinc-900">
          <div className="flex justify-between py-0.5"><span>Subtotal</span><Price value={order.subtotal} /></div>
          <div className="flex justify-between py-0.5 text-emerald-600"><span>Discount {order.couponCode && `(${order.couponCode})`}</span><span>−{order.discountAmount.toLocaleString()}</span></div>
          <div className="flex justify-between py-0.5"><span>Shipping</span><Price value={order.shippingFee} /></div>
          <div className="mt-1 flex justify-between border-t border-zinc-200 pt-2 font-extrabold dark:border-zinc-700">
            <span>Total</span><Price value={order.total} className="text-brand-600 dark:text-brand-400" />
          </div>
        </div>
      </div>

      <div className="flex flex-wrap gap-3">
        <a href={`${apiBase}/api/v1/orders/${order.orderNumber}/invoice`} target="_blank" rel="noopener"
          className="rounded-full border border-zinc-300 px-6 py-2.5 text-sm font-semibold dark:border-zinc-700">
          🧾 View invoice
        </a>
        {order.canBeCancelled && (
          <button
            onClick={async () => {
              if (!confirm("Cancel this order? Any payment will be refunded.")) return;
              await cancel.mutateAsync(order.orderNumber);
              toast("Order cancelled");
            }}
            className="rounded-full border border-rose-300 px-6 py-2.5 text-sm font-semibold text-rose-500 dark:border-rose-500/40">
            Cancel order
          </button>
        )}
      </div>
    </div>
  );
}
