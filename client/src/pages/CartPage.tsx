import { Link } from "react-router-dom";
import { useCart, useCartMutations } from "../api/hooks";
import { useAuth } from "../stores/auth";
import { EmptyState, Price, QuantityStepper, Spinner } from "../components/ui";

export default function CartPage() {
  const user = useAuth((s) => s.user);
  const { data: cart, isLoading } = useCart();
  const { update, remove } = useCartMutations();

  if (!user)
    return <EmptyState icon="🛒" title="Sign in to see your cart"
      action={<Link to="/login" className="rounded-full bg-brand-500 px-6 py-2.5 text-sm font-bold text-white">Sign in</Link>} />;
  if (isLoading || !cart) return <Spinner />;
  if (cart.items.length === 0)
    return <EmptyState icon="🛒" title="Your cart is empty" subtitle="Browse the shop and add something you love."
      action={<Link to="/shop" className="rounded-full bg-brand-500 px-6 py-2.5 text-sm font-bold text-white">Start shopping</Link>} />;

  return (
    <div className="mx-auto max-w-4xl pb-10">
      <h1 className="mb-6 text-2xl font-extrabold">Your cart ({cart.itemCount})</h1>

      <div className="space-y-3">
        {cart.items.map((item) => (
          <div key={item.id} className="flex items-center gap-4 rounded-2xl border border-zinc-200 p-4 dark:border-zinc-800">
            <Link to={`/products/${item.productSlug}`} className="h-20 w-20 shrink-0 overflow-hidden rounded-xl bg-zinc-100 dark:bg-zinc-800">
              {item.imageUrl && <img src={item.imageUrl} alt="" className="h-full w-full object-cover" />}
            </Link>
            <div className="min-w-0 flex-1">
              <Link to={`/products/${item.productSlug}`} className="block truncate font-semibold hover:text-brand-500">
                {item.productName}
              </Link>
              {item.variantLabel && <p className="text-sm text-zinc-500">{item.variantLabel}</p>}
              <Price value={item.unitPrice} className="text-sm text-brand-600 dark:text-brand-400" />
            </div>
            <QuantityStepper value={item.quantity} max={item.stockAvailable}
              onChange={(n) => n < 1
                ? remove.mutate(item.id)
                : update.mutate({ itemId: item.id, quantity: n })} />
            <Price value={item.lineTotal} className="hidden w-24 text-right sm:block" />
            <button onClick={() => remove.mutate(item.id)} aria-label="Remove"
              className="text-zinc-400 transition hover:text-rose-500">✕</button>
          </div>
        ))}
      </div>

      <div className="mt-6 flex flex-col items-end gap-3">
        <p className="text-lg">Subtotal: <Price value={cart.subtotal} className="text-xl" /></p>
        <p className="text-sm text-zinc-500">Shipping and discounts calculated at checkout.</p>
        <Link to="/checkout"
          className="rounded-full bg-gradient-to-r from-brand-500 to-accent-500 px-8 py-3 font-bold text-white transition hover:opacity-90">
          Proceed to checkout →
        </Link>
      </div>
    </div>
  );
}
