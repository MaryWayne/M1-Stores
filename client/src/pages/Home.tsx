import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useProducts } from "../api/hooks";
import { ProductCard } from "../components/ProductCard";
import { Spinner } from "../components/ui";

const CATEGORIES = [
  { slug: "shoes", name: "Shoes", emoji: "👠" },
  { slug: "handbags", name: "Handbags", emoji: "👜" },
  { slug: "cosmetics", name: "Cosmetics", emoji: "💄" },
  { slug: "jewelry", name: "Jewelry", emoji: "💎" },
  { slug: "accessories", name: "Accessories", emoji: "🕶️" },
];

export default function Home() {
  const featured = useProducts({ featured: true, pageSize: 8 });
  const latest = useProducts({ sort: "newest", pageSize: 4 });

  return (
    <div className="space-y-14 pb-10">
      {/* Hero */}
      <section className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-brand-500 via-brand-600 to-accent-600 px-6 py-16 text-center text-white sm:py-24">
        <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.6 }}>
          <p className="text-sm font-semibold uppercase tracking-widest text-white/80">New season, new you</p>
          <h1 className="mx-auto mt-3 max-w-2xl text-4xl font-extrabold tracking-tight sm:text-5xl">
            Style, delivered to your door.
          </h1>
          <p className="mx-auto mt-4 max-w-xl text-white/85">
            Shoes, handbags, cosmetics, jewelry &amp; accessories — with M-Pesa checkout,
            order tracking and free delivery on orders over KES 10,000.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3">
            <Link to="/shop" className="rounded-full bg-white px-7 py-3 text-sm font-bold text-brand-600 transition hover:scale-105">
              Shop now
            </Link>
            <Link to="/shop?featured=true" className="rounded-full border border-white/40 px-7 py-3 text-sm font-bold text-white transition hover:bg-white/10">
              Featured picks
            </Link>
          </div>
        </motion.div>
      </section>

      {/* Categories */}
      <section>
        <h2 className="mb-5 text-xl font-extrabold">Shop by category</h2>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
          {CATEGORIES.map((c, i) => (
            <motion.div key={c.slug} initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.05 }}>
              <Link to={`/shop?categorySlug=${c.slug}`}
                className="flex flex-col items-center gap-2 rounded-2xl border border-zinc-200 py-6 transition hover:border-brand-400 hover:shadow-md dark:border-zinc-800">
                <span className="text-3xl">{c.emoji}</span>
                <span className="text-sm font-semibold">{c.name}</span>
              </Link>
            </motion.div>
          ))}
        </div>
      </section>

      {/* Featured */}
      <section>
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-xl font-extrabold">Featured products</h2>
          <Link to="/shop?featured=true" className="text-sm font-semibold text-brand-500">View all →</Link>
        </div>
        {featured.isLoading ? <Spinner /> : (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {featured.data?.items.map((p) => <ProductCard key={p.id} product={p} />)}
          </div>
        )}
      </section>

      {/* New arrivals */}
      <section>
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-xl font-extrabold">New arrivals</h2>
          <Link to="/shop?sort=newest" className="text-sm font-semibold text-brand-500">View all →</Link>
        </div>
        {latest.isLoading ? <Spinner /> : (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            {latest.data?.items.map((p) => <ProductCard key={p.id} product={p} />)}
          </div>
        )}
      </section>

      {/* Value props */}
      <section className="grid gap-4 sm:grid-cols-3">
        {[["🚚", "Fast delivery", "Countrywide shipping, free over KES 10,000"],
          ["📱", "M-Pesa & cards", "Pay the way that works for you"],
          ["↩️", "Easy returns", "Cancel before dispatch for a full refund"]].map(([icon, title, sub]) => (
          <div key={title} className="flex items-center gap-4 rounded-2xl bg-zinc-50 p-5 dark:bg-zinc-900">
            <span className="text-3xl">{icon}</span>
            <div>
              <p className="font-bold">{title}</p>
              <p className="text-sm text-zinc-500">{sub}</p>
            </div>
          </div>
        ))}
      </section>
    </div>
  );
}
