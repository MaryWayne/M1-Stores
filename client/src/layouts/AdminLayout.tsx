import { Link, NavLink, Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../stores/auth";
import { useTheme } from "../stores/theme";
import { ToastHost } from "../components/ui";

const NAV = [
  ["/admin", "📊", "Dashboard"],
  ["/admin/products", "🏷️", "Products"],
  ["/admin/orders", "📦", "Orders"],
  ["/admin/customers", "👥", "Customers"],
  ["/admin/coupons", "🎟️", "Coupons"],
] as const;

export default function AdminLayout() {
  const { user, logout } = useAuth();
  const { theme, toggle } = useTheme();

  if (!user) return <Navigate to="/login" replace />;
  if (user.role !== "Admin") return <Navigate to="/" replace />;

  return (
    <div className="flex min-h-screen bg-zinc-50 text-zinc-900 dark:bg-zinc-950 dark:text-zinc-50">
      <aside className="sticky top-0 flex h-screen w-16 shrink-0 flex-col border-r border-zinc-200 bg-white py-4 sm:w-56 dark:border-zinc-800 dark:bg-zinc-900">
        <Link to="/" className="mb-6 flex items-center gap-2 px-3 sm:px-5">
          <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-xs font-black text-white">M1</span>
          <span className="hidden text-sm font-extrabold sm:block">Admin</span>
        </Link>
        <nav className="flex flex-1 flex-col gap-1 px-2">
          {NAV.map(([to, icon, label]) => (
            <NavLink key={to} to={to} end={to === "/admin"}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition ${
                  isActive
                    ? "bg-brand-50 text-brand-600 dark:bg-brand-500/10 dark:text-brand-400"
                    : "text-zinc-600 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800"}`}>
              <span className="text-lg">{icon}</span>
              <span className="hidden sm:block">{label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="flex flex-col gap-1 px-2">
          <button onClick={toggle} className="flex items-center gap-3 rounded-xl px-3 py-2 text-sm text-zinc-600 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800">
            <span className="text-lg">{theme === "dark" ? "🌞" : "🌙"}</span>
            <span className="hidden sm:block">Theme</span>
          </button>
          <Link to="/" className="flex items-center gap-3 rounded-xl px-3 py-2 text-sm text-zinc-600 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800">
            <span className="text-lg">🛍️</span>
            <span className="hidden sm:block">View store</span>
          </Link>
          <button onClick={logout} className="flex items-center gap-3 rounded-xl px-3 py-2 text-sm text-rose-500 hover:bg-zinc-100 dark:hover:bg-zinc-800">
            <span className="text-lg">↩️</span>
            <span className="hidden sm:block">Sign out</span>
          </button>
        </div>
      </aside>

      <main className="min-w-0 flex-1 p-4 sm:p-8">
        <Outlet />
      </main>
      <ToastHost />
    </div>
  );
}
