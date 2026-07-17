import { useSearchParams } from "react-router-dom";
import { useCategories, useProducts } from "../api/hooks";
import { ProductCard } from "../components/ProductCard";
import { EmptyState, Pagination, Spinner } from "../components/ui";

const SORTS = [
  ["newest", "Newest"],
  ["price-asc", "Price: low → high"],
  ["price-desc", "Price: high → low"],
  ["rating", "Top rated"],
  ["popular", "Most popular"],
] as const;

export default function Shop() {
  const [params, setParams] = useSearchParams();
  const filters = {
    search: params.get("search") ?? undefined,
    categorySlug: params.get("categorySlug") ?? undefined,
    minPrice: params.get("minPrice") ? Number(params.get("minPrice")) : undefined,
    maxPrice: params.get("maxPrice") ? Number(params.get("maxPrice")) : undefined,
    inStock: params.get("inStock") === "true" ? true : undefined,
    featured: params.get("featured") === "true" ? true : undefined,
    sort: params.get("sort") ?? "newest",
    page: Number(params.get("page") ?? 1),
    pageSize: 12,
  };

  const { data, isLoading } = useProducts(filters);
  const { data: categories } = useCategories();

  const setParam = (key: string, value: string | undefined) => {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value); else next.delete(key);
    if (key !== "page") next.delete("page");
    setParams(next, { replace: true });
  };

  return (
    <div className="flex flex-col gap-6 lg:flex-row">
      {/* Filters sidebar */}
      <aside className="shrink-0 space-y-6 lg:w-56">
        <div>
          <h3 className="mb-2 text-sm font-bold uppercase tracking-wide text-zinc-400">Category</h3>
          <div className="flex flex-wrap gap-2 lg:flex-col lg:gap-1">
            <button onClick={() => setParam("categorySlug", undefined)}
              className={`rounded-full px-3 py-1.5 text-left text-sm lg:rounded-lg ${!filters.categorySlug ? "bg-brand-50 font-semibold text-brand-600 dark:bg-brand-500/10 dark:text-brand-400" : "hover:bg-zinc-100 dark:hover:bg-zinc-800"}`}>
              All products
            </button>
            {categories?.map((c) => (
              <button key={c.slug} onClick={() => setParam("categorySlug", c.slug)}
                className={`rounded-full px-3 py-1.5 text-left text-sm lg:rounded-lg ${filters.categorySlug === c.slug ? "bg-brand-50 font-semibold text-brand-600 dark:bg-brand-500/10 dark:text-brand-400" : "hover:bg-zinc-100 dark:hover:bg-zinc-800"}`}>
                {c.name}
              </button>
            ))}
          </div>
        </div>

        <div>
          <h3 className="mb-2 text-sm font-bold uppercase tracking-wide text-zinc-400">Price (KES)</h3>
          <div className="flex items-center gap-2">
            <input type="number" placeholder="Min" defaultValue={filters.minPrice}
              onBlur={(e) => setParam("minPrice", e.target.value || undefined)}
              className="w-full rounded-lg border border-zinc-200 bg-transparent px-2.5 py-1.5 text-sm dark:border-zinc-700" />
            <span className="text-zinc-400">–</span>
            <input type="number" placeholder="Max" defaultValue={filters.maxPrice}
              onBlur={(e) => setParam("maxPrice", e.target.value || undefined)}
              className="w-full rounded-lg border border-zinc-200 bg-transparent px-2.5 py-1.5 text-sm dark:border-zinc-700" />
          </div>
        </div>

        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={filters.inStock === true}
            onChange={(e) => setParam("inStock", e.target.checked ? "true" : undefined)}
            className="h-4 w-4 accent-brand-500" />
          In stock only
        </label>
      </aside>

      {/* Results */}
      <div className="flex-1">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-zinc-500">
            {data ? `${data.totalCount} product${data.totalCount === 1 ? "" : "s"}` : "…"}
            {filters.search && <> for “<span className="font-semibold text-zinc-800 dark:text-zinc-200">{filters.search}</span>”</>}
          </p>
          <select value={filters.sort} onChange={(e) => setParam("sort", e.target.value)}
            className="rounded-lg border border-zinc-200 bg-white px-3 py-1.5 text-sm dark:border-zinc-700 dark:bg-zinc-900">
            {SORTS.map(([v, label]) => <option key={v} value={v}>{label}</option>)}
          </select>
        </div>

        {isLoading ? (
          <Spinner />
        ) : data && data.items.length > 0 ? (
          <>
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">
              {data.items.map((p) => <ProductCard key={p.id} product={p} />)}
            </div>
            <Pagination page={data.page} totalPages={data.totalPages} onPage={(p) => setParam("page", String(p))} />
          </>
        ) : (
          <EmptyState icon="🔍" title="No products found"
            subtitle="Try a different search term or clear some filters." />
        )}
      </div>
    </div>
  );
}
