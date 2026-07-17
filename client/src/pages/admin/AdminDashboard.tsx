import { useState } from "react";
import { Link } from "react-router-dom";
import { useDashboard, useSalesReport } from "../../api/adminHooks";
import { fmtKes, Price, Spinner, StatusBadge } from "../../components/ui";

function StatTile({ label, value, sub }: { label: string; value: string; sub?: string }) {
  return (
    <div className="rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-900">
      <p className="text-sm text-zinc-500">{label}</p>
      <p className="mt-1 text-2xl font-extrabold tabular-nums">{value}</p>
      {sub && <p className="mt-0.5 text-xs text-zinc-400">{sub}</p>}
    </div>
  );
}

/**
 * Single-series revenue bar chart (SVG). Per the dataviz method: one axis,
 * no legend (the title names the series), thin rounded bars anchored to the
 * baseline, recessive gridlines, labels in text tokens, per-bar hover tooltip.
 */
function RevenueChart({ data }: { data: { period: string; revenue: number }[] }) {
  const [hover, setHover] = useState<number | null>(null);
  if (data.length === 0)
    return <p className="py-10 text-center text-sm text-zinc-400">No sales in the last 30 days yet.</p>;

  const W = 640, H = 200, pad = { top: 16, right: 8, bottom: 24, left: 44 };
  const innerW = W - pad.left - pad.right;
  const innerH = H - pad.top - pad.bottom;
  const max = Math.max(...data.map((d) => d.revenue)) * 1.1;
  const barW = Math.min(28, (innerW / data.length) * 0.7);
  const x = (i: number) => pad.left + (innerW / data.length) * (i + 0.5);
  const y = (v: number) => pad.top + innerH * (1 - v / max);

  const ticks = [0, 0.5, 1].map((f) => max * f);

  return (
    <div className="relative">
      <svg viewBox={`0 0 ${W} ${H}`} className="w-full" role="img" aria-label="Daily revenue, last 30 days">
        {ticks.map((t) => (
          <g key={t}>
            <line x1={pad.left} x2={W - pad.right} y1={y(t)} y2={y(t)}
              className="stroke-zinc-200 dark:stroke-zinc-800" strokeWidth="1" />
            <text x={pad.left - 6} y={y(t) + 3} textAnchor="end"
              className="fill-zinc-400 text-[9px] tabular-nums">
              {t >= 1000 ? `${Math.round(t / 1000)}k` : Math.round(t)}
            </text>
          </g>
        ))}
        {data.map((d, i) => (
          <g key={d.period}
            onMouseEnter={() => setHover(i)} onMouseLeave={() => setHover(null)}>
            {/* generous invisible hit target */}
            <rect x={x(i) - innerW / data.length / 2} y={pad.top} width={innerW / data.length} height={innerH} fill="transparent" />
            <rect
              x={x(i) - barW / 2} y={y(d.revenue)}
              width={barW} height={Math.max(2, pad.top + innerH - y(d.revenue))}
              rx="4"
              className={hover === i ? "fill-brand-600" : "fill-brand-500"}
            />
          </g>
        ))}
        {data.map((d, i) =>
          (i === 0 || i === data.length - 1) && (
            <text key={d.period} x={x(i)} y={H - 6} textAnchor="middle" className="fill-zinc-400 text-[9px]">
              {d.period.slice(5)}
            </text>
          ))}
      </svg>
      {hover !== null && (
        <div className="pointer-events-none absolute rounded-lg bg-zinc-900 px-3 py-1.5 text-xs text-white shadow-lg dark:bg-zinc-100 dark:text-zinc-900"
          style={{ left: `${(x(hover) / W) * 100}%`, top: 0, transform: "translateX(-50%)" }}>
          <span className="font-semibold">{data[hover].period.slice(5)}</span> · {fmtKes(data[hover].revenue)}
        </div>
      )}
    </div>
  );
}

export default function AdminDashboard() {
  const { data: dash, isLoading } = useDashboard();
  const { data: sales } = useSalesReport();

  if (isLoading || !dash) return <Spinner />;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-extrabold">Dashboard</h1>

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatTile label="Revenue" value={fmtKes(dash.totalRevenue)} sub="paid orders, all time" />
        <StatTile label="Orders" value={String(dash.totalOrders)} sub={`${dash.pendingOrders} need attention`} />
        <StatTile label="Customers" value={String(dash.totalCustomers)} />
        <StatTile label="Avg order value" value={fmtKes(dash.avgOrderValue)} />
      </div>

      <div className="rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-900">
        <h2 className="mb-3 font-bold">Revenue — last 30 days</h2>
        <RevenueChart data={sales ?? []} />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="mb-3 font-bold">Top products</h2>
          {dash.topProducts.length === 0 ? <p className="text-sm text-zinc-400">No sales yet.</p> : (
            <ul className="space-y-2 text-sm">
              {dash.topProducts.map((t) => (
                <li key={t.name} className="flex items-center justify-between gap-2">
                  <span className="truncate">{t.name}</span>
                  <span className="shrink-0 text-zinc-500">{t.unitsSold} sold · <Price value={t.revenue} className="font-semibold" /></span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="mb-3 font-bold">Low stock ⚠️</h2>
          {dash.lowStock.length === 0 ? <p className="text-sm text-zinc-400">All stocked up.</p> : (
            <ul className="space-y-2 text-sm">
              {dash.lowStock.map((l) => (
                <li key={l.variantId} className="flex items-center justify-between gap-2">
                  <span className="truncate">{l.product} <span className="text-zinc-400">{l.variant}</span></span>
                  <span className={`shrink-0 font-bold ${l.stock === 0 ? "text-rose-500" : "text-amber-500"}`}>{l.stock} left</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-2xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-900">
          <h2 className="mb-3 font-bold">Recent orders</h2>
          <ul className="space-y-2 text-sm">
            {dash.recentOrders.map((o) => (
              <li key={o.id} className="flex items-center justify-between gap-2">
                <Link to="/admin/orders" className="truncate font-medium hover:text-brand-500">{o.orderNumber}</Link>
                <span className="flex shrink-0 items-center gap-2">
                  <StatusBadge status={o.status} />
                  <Price value={o.total} />
                </span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}
