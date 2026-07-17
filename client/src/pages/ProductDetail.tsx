import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useCartMutations, useCreateReview, useProduct, useReviews, useWishlistMutations } from "../api/hooks";
import { useAuth } from "../stores/auth";
import { useToasts } from "../stores/toast";
import { EmptyState, Price, QuantityStepper, Rating, Spinner } from "../components/ui";
import { ApiError } from "../api/client";

export default function ProductDetail() {
  const { slug } = useParams();
  const { data: product, isLoading, isError } = useProduct(slug!);
  const { data: reviews } = useReviews(product?.id);
  const { add } = useCartMutations();
  const wishlist = useWishlistMutations();
  const user = useAuth((s) => s.user);
  const toast = useToasts((s) => s.push);
  const navigate = useNavigate();

  const [imageIndex, setImageIndex] = useState(0);
  const [variantId, setVariantId] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [reviewOpen, setReviewOpen] = useState(false);

  if (isLoading) return <Spinner />;
  if (isError || !product) return <EmptyState icon="😕" title="Product not found" action={<Link to="/shop" className="text-brand-500 font-semibold">Back to shop</Link>} />;

  const selected = product.variants.find((v) => v.id === variantId) ?? product.variants[0];
  const requireAuth = () => {
    if (user) return true;
    toast("Sign in to continue", "info");
    navigate("/login");
    return false;
  };

  const addToCart = async () => {
    if (!requireAuth() || !selected) return;
    try {
      await add.mutateAsync({ variantId: selected.id, quantity });
      toast("Added to cart 🛒");
    } catch (e) {
      toast(e instanceof ApiError ? e.message : "Could not add to cart", "error");
    }
  };

  return (
    <div className="space-y-12 pb-10">
      <nav className="text-sm text-zinc-500">
        <Link to="/shop" className="hover:text-brand-500">Shop</Link> ·{" "}
        <Link to={`/shop?categorySlug=${product.categorySlug}`} className="hover:text-brand-500">{product.category}</Link> ·{" "}
        <span className="text-zinc-800 dark:text-zinc-200">{product.name}</span>
      </nav>

      <div className="grid gap-8 lg:grid-cols-2">
        {/* Gallery */}
        <div>
          <div className="aspect-square overflow-hidden rounded-3xl bg-zinc-100 dark:bg-zinc-800">
            {product.images[imageIndex] ? (
              <img src={product.images[imageIndex].url} alt={product.images[imageIndex].altText}
                className="h-full w-full object-cover" />
            ) : (
              <div className="flex h-full items-center justify-center text-6xl">🛍️</div>
            )}
          </div>
          {product.images.length > 1 && (
            <div className="mt-3 flex gap-2 overflow-x-auto">
              {product.images.map((img, i) => (
                <button key={img.id} onClick={() => setImageIndex(i)}
                  className={`h-16 w-16 shrink-0 overflow-hidden rounded-xl border-2 ${i === imageIndex ? "border-brand-500" : "border-transparent opacity-70"}`}>
                  <img src={img.url} alt="" className="h-full w-full object-cover" />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Info */}
        <div>
          <p className="text-sm font-semibold uppercase tracking-wide text-zinc-400">
            {product.brand ?? product.category}
          </p>
          <h1 className="mt-1 text-3xl font-extrabold">{product.name}</h1>
          <div className="mt-2 flex items-center gap-3">
            {product.reviewCount > 0
              ? <Rating value={product.avgRating} count={product.reviewCount} />
              : <span className="text-sm text-zinc-400">No reviews yet</span>}
          </div>
          <Price value={selected?.price ?? product.price} className="mt-4 block text-3xl text-brand-600 dark:text-brand-400" />

          <p className="mt-4 leading-relaxed text-zinc-600 dark:text-zinc-400">{product.description}</p>

          {/* Variant picker */}
          {product.variants.some((v) => v.size || v.color) && (
            <div className="mt-6">
              <p className="mb-2 text-sm font-bold">Choose an option</p>
              <div className="flex flex-wrap gap-2">
                {product.variants.map((v) => (
                  <button key={v.id} onClick={() => setVariantId(v.id)} disabled={v.stock === 0}
                    className={`rounded-full border px-4 py-2 text-sm font-medium transition ${
                      (selected?.id === v.id)
                        ? "border-brand-500 bg-brand-50 text-brand-600 dark:bg-brand-500/10 dark:text-brand-400"
                        : "border-zinc-200 hover:border-zinc-400 dark:border-zinc-700"
                    } disabled:cursor-not-allowed disabled:opacity-35`}>
                    {[v.size, v.color].filter(Boolean).join(" / ") || v.sku}
                    {v.stock === 0 && " · out"}
                  </button>
                ))}
              </div>
            </div>
          )}

          <div className="mt-6 flex flex-wrap items-center gap-3">
            <QuantityStepper value={quantity} max={selected?.stock} onChange={(n) => setQuantity(Math.max(1, n))} />
            <span className="text-sm text-zinc-500">
              {selected && selected.stock > 0 ? `${selected.stock} in stock` : "Out of stock"}
            </span>
          </div>

          <div className="mt-6 flex flex-wrap gap-3">
            <button onClick={addToCart} disabled={!selected || selected.stock === 0 || add.isPending}
              className="rounded-full bg-gradient-to-r from-brand-500 to-accent-500 px-8 py-3 font-bold text-white transition hover:opacity-90 disabled:opacity-40">
              {add.isPending ? "Adding…" : "Add to cart"}
            </button>
            <button
              onClick={() => { if (requireAuth()) { wishlist.add.mutate(product.id); toast("Saved to wishlist 🤍"); } }}
              className="rounded-full border border-zinc-300 px-6 py-3 font-semibold transition hover:border-brand-400 dark:border-zinc-700">
              🤍 Save
            </button>
          </div>
        </div>
      </div>

      {/* Reviews */}
      <section>
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-xl font-extrabold">Reviews ({product.reviewCount})</h2>
          {user && !reviewOpen && (
            <button onClick={() => setReviewOpen(true)} className="text-sm font-semibold text-brand-500">
              Write a review
            </button>
          )}
        </div>

        {reviewOpen && <ReviewForm productId={product.id} onDone={() => setReviewOpen(false)} />}

        {reviews && reviews.items.length > 0 ? (
          <div className="space-y-4">
            {reviews.items.map((r) => (
              <div key={r.id} className="rounded-2xl border border-zinc-200 p-5 dark:border-zinc-800">
                <div className="flex flex-wrap items-center gap-2">
                  <Rating value={r.rating} />
                  <span className="font-semibold">{r.title}</span>
                  {r.isVerifiedPurchase && (
                    <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400">
                      ✓ Verified purchase
                    </span>
                  )}
                </div>
                <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">{r.body}</p>
                <p className="mt-2 text-xs text-zinc-400">
                  {r.userName} · {new Date(r.createdAt).toLocaleDateString()}
                </p>
              </div>
            ))}
          </div>
        ) : (
          !reviewOpen && <p className="text-sm text-zinc-500">Be the first to review this product.</p>
        )}
      </section>
    </div>
  );
}

function ReviewForm({ productId, onDone }: { productId: string; onDone: () => void }) {
  const create = useCreateReview(productId);
  const toast = useToasts((s) => s.push);
  const [rating, setRating] = useState(5);
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await create.mutateAsync({ rating, title, body });
      toast("Thanks for your review ⭐");
      onDone();
    } catch (err) {
      toast(err instanceof ApiError ? err.message : "Could not submit review", "error");
    }
  };

  return (
    <form onSubmit={submit} className="mb-6 space-y-3 rounded-2xl border border-brand-200 bg-brand-50/50 p-5 dark:border-brand-500/30 dark:bg-brand-500/5">
      <div className="flex items-center gap-1 text-2xl">
        {[1, 2, 3, 4, 5].map((n) => (
          <button key={n} type="button" onClick={() => setRating(n)}
            className={n <= rating ? "text-amber-500" : "text-zinc-300 dark:text-zinc-600"}>★</button>
        ))}
      </div>
      <input value={title} onChange={(e) => setTitle(e.target.value)} required placeholder="Title"
        className="w-full rounded-lg border border-zinc-200 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900" />
      <textarea value={body} onChange={(e) => setBody(e.target.value)} required rows={3} placeholder="What did you think?"
        className="w-full rounded-lg border border-zinc-200 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-900" />
      <div className="flex gap-2">
        <button type="submit" disabled={create.isPending}
          className="rounded-full bg-brand-500 px-5 py-2 text-sm font-bold text-white disabled:opacity-40">
          {create.isPending ? "Posting…" : "Post review"}
        </button>
        <button type="button" onClick={onDone} className="rounded-full px-4 py-2 text-sm text-zinc-500">Cancel</button>
      </div>
    </form>
  );
}
