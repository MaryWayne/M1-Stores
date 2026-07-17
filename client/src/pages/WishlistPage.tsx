import { Link } from "react-router-dom";
import { useWishlist, useWishlistMutations } from "../api/hooks";
import { useAuth } from "../stores/auth";
import { EmptyState, Price, Rating, Spinner } from "../components/ui";

export default function WishlistPage() {
  const user = useAuth((s) => s.user);
  const { data, isLoading } = useWishlist();
  const { remove } = useWishlistMutations();

  if (!user)
    return <EmptyState icon="🤍" title="Sign in to see your wishlist"
      action={<Link to="/login" className="rounded-full bg-brand-500 px-6 py-2.5 text-sm font-bold text-white">Sign in</Link>} />;
  if (isLoading) return <Spinner />;
  if (!data || data.length === 0)
    return <EmptyState icon="🤍" title="Your wishlist is empty" subtitle="Tap the heart on any product to save it here." />;

  return (
    <div className="mx-auto max-w-4xl pb-10">
      <h1 className="mb-6 text-2xl font-extrabold">Wishlist ({data.length})</h1>
      <div className="space-y-3">
        {data.map((w) => (
          <div key={w.productId} className="flex items-center gap-4 rounded-2xl border border-zinc-200 p-4 dark:border-zinc-800">
            <Link to={`/products/${w.slug}`} className="h-20 w-20 shrink-0 overflow-hidden rounded-xl bg-zinc-100 dark:bg-zinc-800">
              {w.imageUrl && <img src={w.imageUrl} alt="" className="h-full w-full object-cover" />}
            </Link>
            <div className="min-w-0 flex-1">
              <Link to={`/products/${w.slug}`} className="block truncate font-semibold hover:text-brand-500">{w.name}</Link>
              <Rating value={w.avgRating} size="text-xs" />
              <Price value={w.price} className="text-sm text-brand-600 dark:text-brand-400" />
            </div>
            <span className={`text-xs font-semibold ${w.inStock ? "text-emerald-600" : "text-zinc-400"}`}>
              {w.inStock ? "In stock" : "Out of stock"}
            </span>
            <Link to={`/products/${w.slug}`} className="rounded-full bg-brand-500 px-4 py-2 text-xs font-bold text-white">View</Link>
            <button onClick={() => remove.mutate(w.productId)} aria-label="Remove" className="text-zinc-400 hover:text-rose-500">✕</button>
          </div>
        ))}
      </div>
    </div>
  );
}
