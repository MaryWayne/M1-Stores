import { useState } from "react";
import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../stores/auth";
import { useTheme } from "../stores/theme";
import { useCart, useNotifications } from "../api/hooks";
import { ToastHost } from "../components/ui";

function IconButton({ to, label, children, badge }: {
  to: string; label: string; children: React.ReactNode; badge?: number;
}) {
  return (
    <Link to={to} aria-label={label}
      className="relative rounded-full p-2 text-xl transition hover:bg-zinc-100 dark:hover:bg-zinc-800">
      {children}
      {badge != null && badge > 0 && (
        <span className="absolute -right-0.5 -top-0.5 flex h-5 min-w-5 items-center justify-center rounded-full bg-brand-500 px-1 text-[11px] font-bold text-white">
          {badge > 9 ? "9+" : badge}
        </span>
      )}
    </Link>
  );
}

export default function StorefrontLayout() {
  const { user, logout } = useAuth();
  const { theme, toggle } = useTheme();
  const { data: cart } = useCart();
  const { data: notifications } = useNotifications();
  const unread = notifications?.items.filter((n) => !n.isRead).length ?? 0;
  const [search, setSearch] = useState("");
  const [menuOpen, setMenuOpen] = useState(false);
  const navigate = useNavigate();

  const submitSearch = (e: React.FormEvent) => {
    e.preventDefault();
    navigate(`/shop${search ? `?search=${encodeURIComponent(search)}` : ""}`);
  };

  const navLink = ({ isActive }: { isActive: boolean }) =>
    `text-sm font-medium transition ${isActive ? "text-brand-500" : "text-zinc-600 hover:text-zinc-900 dark:text-zinc-400 dark:hover:text-zinc-100"}`;

  return (
    <div className="min-h-screen bg-white text-zinc-900 transition-colors dark:bg-zinc-950 dark:text-zinc-50">
      <header className="sticky top-0 z-40 border-b border-zinc-100 bg-white/85 backdrop-blur dark:border-zinc-800 dark:bg-zinc-950/85">
        <div className="mx-auto flex max-w-7xl items-center gap-4 px-4 py-3 sm:px-6">
          <button className="text-2xl sm:hidden" onClick={() => setMenuOpen(!menuOpen)} aria-label="Menu">☰</button>

          <Link to="/" className="flex shrink-0 items-center gap-2 text-lg font-extrabold">
            <span className="flex h-8 w-8 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-xs font-black text-white">M1</span>
            <span className="hidden sm:inline">M1 Stores</span>
          </Link>

          <nav className="hidden items-center gap-5 sm:flex">
            <NavLink to="/shop" className={navLink}>Shop</NavLink>
            <NavLink to="/shop?categorySlug=shoes" className={navLink}>Shoes</NavLink>
            <NavLink to="/shop?categorySlug=handbags" className={navLink}>Bags</NavLink>
            <NavLink to="/shop?categorySlug=cosmetics" className={navLink}>Beauty</NavLink>
            <NavLink to="/shop?categorySlug=jewelry" className={navLink}>Jewelry</NavLink>
          </nav>

          <form onSubmit={submitSearch} className="ml-auto hidden flex-1 max-w-xs md:block">
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search products…"
              className="w-full rounded-full border border-zinc-200 bg-zinc-50 px-4 py-2 text-sm outline-none transition focus:border-brand-400 dark:border-zinc-700 dark:bg-zinc-900"
            />
          </form>

          <div className="ml-auto flex items-center gap-1 md:ml-0">
            <button onClick={toggle} aria-label="Toggle theme"
              className="rounded-full p-2 text-xl transition hover:bg-zinc-100 dark:hover:bg-zinc-800">
              {theme === "dark" ? "🌞" : "🌙"}
            </button>
            {user && <IconButton to="/notifications" label="Notifications" badge={unread}>🔔</IconButton>}
            <IconButton to="/wishlist" label="Wishlist">🤍</IconButton>
            <IconButton to="/cart" label="Cart" badge={cart?.itemCount}>🛒</IconButton>
            {user ? (
              <div className="group relative">
                <button className="ml-1 flex h-9 w-9 items-center justify-center rounded-full bg-gradient-to-br from-brand-500 to-accent-500 text-sm font-bold text-white">
                  {user.fullName.slice(0, 1)}
                </button>
                <div className="invisible absolute right-0 top-full z-50 mt-1 w-48 rounded-xl border border-zinc-200 bg-white p-1.5 opacity-0 shadow-xl transition group-hover:visible group-hover:opacity-100 dark:border-zinc-700 dark:bg-zinc-900">
                  <p className="truncate px-3 py-2 text-xs text-zinc-500">{user.email}</p>
                  <Link to="/account" className="block rounded-lg px-3 py-2 text-sm hover:bg-zinc-100 dark:hover:bg-zinc-800">My account</Link>
                  <Link to="/orders" className="block rounded-lg px-3 py-2 text-sm hover:bg-zinc-100 dark:hover:bg-zinc-800">My orders</Link>
                  {user.role === "Admin" && (
                    <Link to="/admin" className="block rounded-lg px-3 py-2 text-sm font-semibold text-brand-500 hover:bg-zinc-100 dark:hover:bg-zinc-800">Admin dashboard</Link>
                  )}
                  <button onClick={() => { logout(); navigate("/"); }}
                    className="block w-full rounded-lg px-3 py-2 text-left text-sm text-rose-500 hover:bg-zinc-100 dark:hover:bg-zinc-800">
                    Sign out
                  </button>
                </div>
              </div>
            ) : (
              <Link to="/login" className="ml-2 rounded-full bg-gradient-to-r from-brand-500 to-accent-500 px-4 py-2 text-sm font-semibold text-white">
                Sign in
              </Link>
            )}
          </div>
        </div>

        {menuOpen && (
          <nav className="flex flex-col gap-1 border-t border-zinc-100 px-4 py-3 sm:hidden dark:border-zinc-800">
            {[["/shop", "Shop all"], ["/shop?categorySlug=shoes", "Shoes"], ["/shop?categorySlug=handbags", "Handbags"],
              ["/shop?categorySlug=cosmetics", "Cosmetics"], ["/shop?categorySlug=jewelry", "Jewelry"],
              ["/shop?categorySlug=accessories", "Accessories"]].map(([to, label]) => (
              <Link key={to} to={to} onClick={() => setMenuOpen(false)}
                className="rounded-lg px-3 py-2 text-sm font-medium hover:bg-zinc-100 dark:hover:bg-zinc-800">
                {label}
              </Link>
            ))}
          </nav>
        )}
      </header>

      <main className="mx-auto min-h-[70vh] max-w-7xl px-4 py-6 sm:px-6">
        <Outlet />
      </main>

      <footer className="border-t border-zinc-100 py-10 dark:border-zinc-800">
        <div className="mx-auto flex max-w-7xl flex-col items-center gap-3 px-6 text-center text-sm text-zinc-500">
          <p className="font-bold text-zinc-700 dark:text-zinc-300">M1 Stores</p>
          <p>Shoes · Handbags · Cosmetics · Jewelry · Accessories</p>
          <p>M-Pesa &amp; card payments · Fast delivery across Kenya</p>
          <p className="text-xs">© 2026 M1 Stores · Built by Mary Wainaina · WayneTech Studio</p>
        </div>
      </footer>

      <ToastHost />
    </div>
  );
}
