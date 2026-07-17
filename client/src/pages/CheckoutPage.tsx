import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAddresses, useAddressMutations, useCart, useCheckout, useQuote } from "../api/hooks";
import { useToasts } from "../stores/toast";
import { ApiError } from "../api/client";
import { EmptyState, Price, Spinner } from "../components/ui";

const PAYMENT_METHODS = [
  { value: 0, label: "Demo payment", sub: "Instant success — perfect for trying the store", icon: "✨" },
  { value: 1, label: "M-Pesa", sub: "STK push to your phone", icon: "📱" },
  { value: 2, label: "Card (Stripe)", sub: "Visa / Mastercard", icon: "💳" },
];

export default function CheckoutPage() {
  const { data: cart, isLoading: cartLoading } = useCart();
  const { data: addresses } = useAddresses();
  const addressMutations = useAddressMutations();
  const checkout = useCheckout();
  const toast = useToasts((s) => s.push);
  const navigate = useNavigate();

  const [addressId, setAddressId] = useState<string | null>(null);
  const [couponInput, setCouponInput] = useState("");
  const [coupon, setCoupon] = useState<string | null>(null);
  const [payment, setPayment] = useState(0);
  const [mpesaPhone, setMpesaPhone] = useState("");
  const [addingAddress, setAddingAddress] = useState(false);

  const quote = useQuote(coupon, !!cart && cart.items.length > 0);
  const selectedAddress = addressId ?? addresses?.find((a) => a.isDefault)?.id ?? addresses?.[0]?.id ?? null;

  if (cartLoading) return <Spinner />;
  if (!cart || cart.items.length === 0)
    return <EmptyState icon="🧺" title="Nothing to check out"
      action={<Link to="/shop" className="rounded-full bg-brand-500 px-6 py-2.5 text-sm font-bold text-white">Go shopping</Link>} />;

  const placeOrder = async () => {
    if (!selectedAddress) { toast("Add a delivery address first", "error"); return; }
    try {
      const result = await checkout.mutateAsync({
        addressId: selectedAddress,
        couponCode: quote.data?.couponCode ?? null,
        paymentProvider: payment,
        mpesaPhone: payment === 1 ? mpesaPhone : null,
      });
      if (result.redirectUrl) { window.location.href = result.redirectUrl; return; }
      toast(`Order ${result.order.orderNumber} placed 🎉`);
      navigate(`/orders/${result.order.orderNumber}`);
    } catch (e) {
      toast(e instanceof ApiError ? e.message : "Checkout failed", "error");
    }
  };

  return (
    <div className="mx-auto grid max-w-5xl gap-8 pb-10 lg:grid-cols-[1fr_360px]">
      <div className="space-y-8">
        {/* Address */}
        <section>
          <h2 className="mb-3 text-lg font-extrabold">1 · Delivery address</h2>
          <div className="space-y-2">
            {addresses?.map((a) => (
              <label key={a.id} className={`flex cursor-pointer items-start gap-3 rounded-2xl border p-4 transition ${selectedAddress === a.id ? "border-brand-500 bg-brand-50/50 dark:bg-brand-500/5" : "border-zinc-200 dark:border-zinc-800"}`}>
                <input type="radio" name="address" checked={selectedAddress === a.id}
                  onChange={() => setAddressId(a.id)} className="mt-1 accent-brand-500" />
                <div className="text-sm">
                  <p className="font-bold">{a.label} {a.isDefault && <span className="text-xs font-semibold text-brand-500">· default</span>}</p>
                  <p>{a.fullName} · {a.phone}</p>
                  <p className="text-zinc-500">{a.line1}, {a.city}, {a.county}</p>
                </div>
              </label>
            ))}
          </div>
          {addingAddress ? (
            <AddressForm
              onSave={async (body) => {
                const saved = await addressMutations.save.mutateAsync({ body });
                setAddressId(saved.id);
                setAddingAddress(false);
              }}
              onCancel={() => setAddingAddress(false)}
            />
          ) : (
            <button onClick={() => setAddingAddress(true)} className="mt-2 text-sm font-semibold text-brand-500">
              + Add a new address
            </button>
          )}
        </section>

        {/* Payment */}
        <section>
          <h2 className="mb-3 text-lg font-extrabold">2 · Payment method</h2>
          <div className="space-y-2">
            {PAYMENT_METHODS.map((m) => (
              <label key={m.value} className={`flex cursor-pointer items-center gap-3 rounded-2xl border p-4 transition ${payment === m.value ? "border-brand-500 bg-brand-50/50 dark:bg-brand-500/5" : "border-zinc-200 dark:border-zinc-800"}`}>
                <input type="radio" name="payment" checked={payment === m.value}
                  onChange={() => setPayment(m.value)} className="accent-brand-500" />
                <span className="text-2xl">{m.icon}</span>
                <div className="text-sm">
                  <p className="font-bold">{m.label}</p>
                  <p className="text-zinc-500">{m.sub}</p>
                </div>
              </label>
            ))}
          </div>
          {payment === 1 && (
            <input value={mpesaPhone} onChange={(e) => setMpesaPhone(e.target.value)}
              placeholder="M-Pesa phone e.g. 0712 345 678"
              className="mt-3 w-full rounded-xl border border-zinc-200 bg-transparent px-4 py-2.5 text-sm dark:border-zinc-700" />
          )}
        </section>
      </div>

      {/* Summary */}
      <aside className="h-fit rounded-3xl border border-zinc-200 p-6 dark:border-zinc-800">
        <h2 className="mb-4 text-lg font-extrabold">Order summary</h2>
        <div className="space-y-2 text-sm">
          {cart.items.map((i) => (
            <div key={i.id} className="flex justify-between gap-2">
              <span className="truncate text-zinc-600 dark:text-zinc-400">{i.quantity}× {i.productName}</span>
              <Price value={i.lineTotal} className="font-medium" />
            </div>
          ))}
        </div>

        <div className="mt-4 flex gap-2">
          <input value={couponInput} onChange={(e) => setCouponInput(e.target.value.toUpperCase())}
            placeholder="Coupon code"
            className="w-full rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-700" />
          <button onClick={() => setCoupon(couponInput || null)}
            className="rounded-xl bg-zinc-900 px-4 py-2 text-sm font-semibold text-white dark:bg-zinc-100 dark:text-zinc-900">
            Apply
          </button>
        </div>
        {quote.data?.couponError && <p className="mt-1.5 text-xs text-rose-500">{quote.data.couponError}</p>}
        {quote.data?.couponCode && <p className="mt-1.5 text-xs text-emerald-600">Coupon {quote.data.couponCode} applied ✓</p>}

        {quote.data && (
          <div className="mt-4 space-y-1.5 border-t border-zinc-100 pt-4 text-sm dark:border-zinc-800">
            <div className="flex justify-between"><span>Subtotal</span><Price value={quote.data.subtotal} /></div>
            <div className="flex justify-between text-emerald-600"><span>Discount</span><span>−{quote.data.discountAmount.toLocaleString()}</span></div>
            <div className="flex justify-between">
              <span>Shipping</span>
              {quote.data.shippingFee === 0 ? <span className="font-semibold text-emerald-600">FREE</span> : <Price value={quote.data.shippingFee} />}
            </div>
            <div className="flex justify-between border-t border-zinc-100 pt-2 text-base font-extrabold dark:border-zinc-800">
              <span>Total</span><Price value={quote.data.total} className="text-brand-600 dark:text-brand-400" />
            </div>
          </div>
        )}

        <button onClick={placeOrder} disabled={checkout.isPending}
          className="mt-5 w-full rounded-full bg-gradient-to-r from-brand-500 to-accent-500 py-3 font-bold text-white transition hover:opacity-90 disabled:opacity-40">
          {checkout.isPending ? "Placing order…" : "Place order"}
        </button>
        <p className="mt-3 text-center text-xs text-zinc-400">🔒 Secure checkout · Free shipping over KES 10,000</p>
      </aside>
    </div>
  );
}

function AddressForm({ onSave, onCancel }: {
  onSave: (body: { label: string; fullName: string; phone: string; line1: string; city: string; county: string; isDefault: boolean }) => Promise<void>;
  onCancel: () => void;
}) {
  const [form, setForm] = useState({ label: "Home", fullName: "", phone: "", line1: "", city: "", county: "", isDefault: true });
  const [saving, setSaving] = useState(false);
  const field = (key: keyof typeof form, placeholder: string) => (
    <input required value={String(form[key])} placeholder={placeholder}
      onChange={(e) => setForm({ ...form, [key]: e.target.value })}
      className="w-full rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-700" />
  );
  return (
    <form className="mt-3 grid gap-2 rounded-2xl border border-dashed border-zinc-300 p-4 sm:grid-cols-2 dark:border-zinc-700"
      onSubmit={async (e) => { e.preventDefault(); setSaving(true); try { await onSave(form); } finally { setSaving(false); } }}>
      {field("label", "Label (Home, Office…)")}
      {field("fullName", "Full name")}
      {field("phone", "Phone")}
      {field("line1", "Street / building")}
      {field("city", "City / town")}
      {field("county", "County")}
      <div className="flex gap-2 sm:col-span-2">
        <button type="submit" disabled={saving} className="rounded-full bg-brand-500 px-5 py-2 text-sm font-bold text-white disabled:opacity-40">
          {saving ? "Saving…" : "Save address"}
        </button>
        <button type="button" onClick={onCancel} className="rounded-full px-4 py-2 text-sm text-zinc-500">Cancel</button>
      </div>
    </form>
  );
}
