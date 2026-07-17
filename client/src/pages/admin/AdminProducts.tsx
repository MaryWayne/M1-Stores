import { useState } from "react";
import { useCategories, useProducts } from "../../api/hooks";
import { useBrands, useProductAdmin, type SaveProductBody } from "../../api/adminHooks";
import { get } from "../../api/client";
import type { ProductDetail } from "../../api/types";
import { useToasts } from "../../stores/toast";
import { ApiError } from "../../api/client";
import { Pagination, Price, Rating, Spinner } from "../../components/ui";

const inputCls = "w-full rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-700";

export default function AdminProducts() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const { data, isLoading } = useProducts({ search: search || undefined, page, pageSize: 10, sort: "newest" });
  const [editing, setEditing] = useState<ProductDetail | "new" | null>(null);
  const { remove } = useProductAdmin();
  const toast = useToasts((s) => s.push);

  const openEdit = async (slug: string) => setEditing(await get<ProductDetail>(`/products/${slug}`));

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-extrabold">Products</h1>
        <div className="flex gap-2">
          <input value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            placeholder="Search…" className={`${inputCls} w-48`} />
          <button onClick={() => setEditing("new")}
            className="rounded-full bg-gradient-to-r from-brand-500 to-accent-500 px-5 py-2 text-sm font-bold text-white">
            + New product
          </button>
        </div>
      </div>

      {editing && (
        <ProductForm
          product={editing === "new" ? null : editing}
          onClose={() => setEditing(null)}
        />
      )}

      {isLoading || !data ? <Spinner /> : (
        <div className="overflow-x-auto rounded-2xl border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900">
          <table className="w-full min-w-[680px] text-sm">
            <thead>
              <tr className="border-b border-zinc-100 text-left text-xs uppercase tracking-wide text-zinc-400 dark:border-zinc-800">
                <th className="px-4 py-3">Product</th>
                <th className="px-4 py-3">Category</th>
                <th className="px-4 py-3">Price</th>
                <th className="px-4 py-3">Rating</th>
                <th className="px-4 py-3">Stock</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((p) => (
                <tr key={p.id} className="border-b border-zinc-50 last:border-0 dark:border-zinc-800/50">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className="h-10 w-10 shrink-0 overflow-hidden rounded-lg bg-zinc-100 dark:bg-zinc-800">
                        {p.imageUrl && <img src={p.imageUrl} alt="" className="h-full w-full object-cover" />}
                      </div>
                      <div>
                        <p className="font-semibold">{p.name} {p.isFeatured && "⭐"}</p>
                        <p className="text-xs text-zinc-400">{p.brand}</p>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3">{p.category}</td>
                  <td className="px-4 py-3"><Price value={p.price} /></td>
                  <td className="px-4 py-3"><Rating value={p.avgRating} count={p.reviewCount} size="text-xs" /></td>
                  <td className="px-4 py-3">
                    <span className={p.inStock ? "text-emerald-600" : "font-bold text-rose-500"}>
                      {p.inStock ? "In stock" : "Out"}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-2">
                      <button onClick={() => openEdit(p.slug)} className="rounded-full border border-zinc-200 px-3 py-1 text-xs font-semibold dark:border-zinc-700">Edit</button>
                      <button
                        onClick={async () => {
                          if (!confirm(`Retire "${p.name}"? It stays in past orders.`)) return;
                          await remove.mutateAsync(p.id);
                          toast("Product retired");
                        }}
                        className="rounded-full border border-rose-200 px-3 py-1 text-xs font-semibold text-rose-500 dark:border-rose-500/40">
                        Retire
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="px-4 pb-4">
            <Pagination page={data.page} totalPages={data.totalPages} onPage={setPage} />
          </div>
        </div>
      )}
    </div>
  );
}

function ProductForm({ product, onClose }: { product: ProductDetail | null; onClose: () => void }) {
  const { data: categories } = useCategories();
  const { data: brands } = useBrands();
  const { save, updateVariant, addVariant } = useProductAdmin();
  const toast = useToasts((s) => s.push);

  const [form, setForm] = useState<SaveProductBody>({
    name: product?.name ?? "",
    description: product?.description ?? "",
    categoryId: "",
    brandId: null,
    basePrice: product?.price ?? 0,
    isFeatured: product?.isFeatured ?? false,
  });
  const [catReady, setCatReady] = useState(false);

  // Resolve category/brand ids from names once lookups arrive.
  if (!catReady && categories && brands) {
    const cat = categories.find((c) => c.name === product?.category) ?? categories[0];
    const brand = brands.find((b) => b.name === product?.brand);
    setForm((f) => ({ ...f, categoryId: cat?.id ?? "", brandId: brand?.id ?? null }));
    setCatReady(true);
  }

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await save.mutateAsync({ id: product?.id, body: form });
      toast(product ? "Product updated ✓" : "Product created ✓");
      onClose();
    } catch (err) {
      toast(err instanceof ApiError ? err.message : "Save failed", "error");
    }
  };

  return (
    <form onSubmit={submit} className="space-y-3 rounded-2xl border border-brand-200 bg-white p-5 dark:border-brand-500/30 dark:bg-zinc-900">
      <h2 className="font-bold">{product ? `Edit: ${product.name}` : "New product"}</h2>
      <div className="grid gap-3 sm:grid-cols-2">
        <input required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="Name" className={inputCls} />
        <input required type="number" min="1" value={form.basePrice || ""} onChange={(e) => setForm({ ...form, basePrice: Number(e.target.value) })} placeholder="Base price (KES)" className={inputCls} />
        <select value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })} className={inputCls}>
          {categories?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        <select value={form.brandId ?? ""} onChange={(e) => setForm({ ...form, brandId: e.target.value || null })} className={inputCls}>
          <option value="">No brand</option>
          {brands?.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
        </select>
      </div>
      <textarea required rows={2} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder="Description" className={inputCls} />
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" checked={form.isFeatured} onChange={(e) => setForm({ ...form, isFeatured: e.target.checked })} className="h-4 w-4 accent-brand-500" />
        Featured on the home page
      </label>

      {product && (
        <div>
          <h3 className="mb-2 text-sm font-bold">Variants & stock</h3>
          <div className="space-y-2">
            {product.variants.map((v) => (
              <VariantRow key={v.id} variant={v}
                onSave={async (stock) => {
                  await updateVariant.mutateAsync({ variantId: v.id, size: v.size, color: v.color, priceOverride: null, stock });
                  toast("Stock updated ✓");
                }} />
            ))}
          </div>
          <button type="button"
            onClick={async () => {
              const size = prompt("Size / option label (leave empty for One Size):") ?? "";
              await addVariant.mutateAsync({ productId: product.id, size: size || null, color: null, stock: 10 });
              toast("Variant added — reopen to see it");
            }}
            className="mt-2 text-sm font-semibold text-brand-500">
            + Add variant
          </button>
        </div>
      )}

      <div className="flex gap-2">
        <button type="submit" disabled={save.isPending}
          className="rounded-full bg-brand-500 px-6 py-2 text-sm font-bold text-white disabled:opacity-40">
          {save.isPending ? "Saving…" : "Save product"}
        </button>
        <button type="button" onClick={onClose} className="rounded-full px-4 py-2 text-sm text-zinc-500">Close</button>
      </div>
    </form>
  );
}

function VariantRow({ variant, onSave }: {
  variant: { id: string; sku: string; size: string | null; color: string | null; price: number; stock: number };
  onSave: (stock: number) => Promise<void>;
}) {
  const [stock, setStock] = useState(variant.stock);
  return (
    <div className="flex items-center gap-3 rounded-xl border border-zinc-100 px-3 py-2 text-sm dark:border-zinc-800">
      <span className="w-24 font-mono text-xs text-zinc-400">{variant.sku}</span>
      <span className="flex-1">{[variant.size, variant.color].filter(Boolean).join(" / ") || "One Size"}</span>
      <Price value={variant.price} className="text-xs" />
      <input type="number" min="0" value={stock} onChange={(e) => setStock(Number(e.target.value))}
        className="w-20 rounded-lg border border-zinc-200 bg-transparent px-2 py-1 text-sm dark:border-zinc-700" />
      {stock !== variant.stock && (
        <button type="button" onClick={() => onSave(stock)} className="rounded-full bg-brand-500 px-3 py-1 text-xs font-bold text-white">Save</button>
      )}
    </div>
  );
}
