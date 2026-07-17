import { AnimatePresence, motion } from "framer-motion";
import { useToasts } from "../stores/toast";

export const fmtKes = (n: number) =>
  `KES ${n.toLocaleString("en-KE", { maximumFractionDigits: 0 })}`;

export function Price({ value, className = "" }: { value: number; className?: string }) {
  return <span className={`font-bold tabular-nums ${className}`}>{fmtKes(value)}</span>;
}

export function Rating({ value, count, size = "text-sm" }: { value: number; count?: number; size?: string }) {
  return (
    <span className={`inline-flex items-center gap-1 ${size}`}>
      <span className="text-amber-500" aria-label={`${value} out of 5 stars`}>
        {"★".repeat(Math.round(value)) + "☆".repeat(5 - Math.round(value))}
      </span>
      {count !== undefined && <span className="text-zinc-500">({count})</span>}
    </span>
  );
}

export function Spinner({ label = "Loading…" }: { label?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-20 text-zinc-500">
      <div className="h-8 w-8 animate-spin rounded-full border-2 border-zinc-300 border-t-brand-500" />
      <p className="text-sm">{label}</p>
    </div>
  );
}

export function EmptyState({ icon, title, subtitle, action }: {
  icon: string; title: string; subtitle?: string; action?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 py-20 text-center">
      <span className="text-5xl">{icon}</span>
      <h3 className="mt-2 text-lg font-bold">{title}</h3>
      {subtitle && <p className="max-w-sm text-sm text-zinc-500">{subtitle}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}

export function StatusBadge({ status }: { status: string }) {
  const styles: Record<string, string> = {
    PendingPayment: "bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400",
    Paid: "bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400",
    Processing: "bg-blue-100 text-blue-700 dark:bg-blue-500/10 dark:text-blue-400",
    Shipped: "bg-violet-100 text-violet-700 dark:bg-violet-500/10 dark:text-violet-400",
    Delivered: "bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400",
    Cancelled: "bg-zinc-100 text-zinc-600 dark:bg-zinc-500/10 dark:text-zinc-400",
    Refunded: "bg-rose-100 text-rose-700 dark:bg-rose-500/10 dark:text-rose-400",
  };
  return (
    <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${styles[status] ?? styles.Cancelled}`}>
      {status.replace(/([A-Z])/g, " $1").trim()}
    </span>
  );
}

export function QuantityStepper({ value, onChange, max }: {
  value: number; onChange: (next: number) => void; max?: number;
}) {
  return (
    <div className="inline-flex items-center rounded-full border border-zinc-200 dark:border-zinc-700">
      <button onClick={() => onChange(value - 1)} className="px-3 py-1.5 text-lg leading-none hover:text-brand-500" aria-label="Decrease">−</button>
      <span className="min-w-8 text-center text-sm font-semibold tabular-nums">{value}</span>
      <button onClick={() => onChange(value + 1)} disabled={max !== undefined && value >= max}
        className="px-3 py-1.5 text-lg leading-none hover:text-brand-500 disabled:opacity-30" aria-label="Increase">+</button>
    </div>
  );
}

export function ToastHost() {
  const { toasts, dismiss } = useToasts();
  const styles = {
    success: "bg-emerald-600",
    error: "bg-rose-600",
    info: "bg-zinc-800",
  };
  return (
    <div className="pointer-events-none fixed bottom-6 left-1/2 z-[100] flex -translate-x-1/2 flex-col gap-2">
      <AnimatePresence>
        {toasts.map((t) => (
          <motion.button
            key={t.id}
            initial={{ opacity: 0, y: 16, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 8, scale: 0.95 }}
            onClick={() => dismiss(t.id)}
            className={`pointer-events-auto rounded-full px-5 py-2.5 text-sm font-medium text-white shadow-lg ${styles[t.kind]}`}
          >
            {t.message}
          </motion.button>
        ))}
      </AnimatePresence>
    </div>
  );
}

export function Pagination({ page, totalPages, onPage }: {
  page: number; totalPages: number; onPage: (p: number) => void;
}) {
  if (totalPages <= 1) return null;
  return (
    <div className="mt-8 flex items-center justify-center gap-2">
      <button disabled={page <= 1} onClick={() => onPage(page - 1)}
        className="rounded-full border border-zinc-200 px-4 py-1.5 text-sm disabled:opacity-30 dark:border-zinc-700">←</button>
      <span className="px-2 text-sm text-zinc-500">Page {page} of {totalPages}</span>
      <button disabled={page >= totalPages} onClick={() => onPage(page + 1)}
        className="rounded-full border border-zinc-200 px-4 py-1.5 text-sm disabled:opacity-30 dark:border-zinc-700">→</button>
    </div>
  );
}
