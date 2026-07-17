import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import type { ProductListItem } from "../api/types";
import { Price, Rating } from "./ui";

export function ProductCard({ product }: { product: ProductListItem }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      className="group overflow-hidden rounded-2xl border border-zinc-200 bg-white transition-shadow hover:shadow-lg dark:border-zinc-800 dark:bg-zinc-900"
    >
      <Link to={`/products/${product.slug}`}>
        <div className="relative aspect-square overflow-hidden bg-zinc-100 dark:bg-zinc-800">
          {product.imageUrl ? (
            <img src={product.imageUrl} alt={product.name} loading="lazy"
              className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105" />
          ) : (
            <div className="flex h-full items-center justify-center text-4xl">🛍️</div>
          )}
          {product.isFeatured && (
            <span className="absolute left-3 top-3 rounded-full bg-brand-500 px-2.5 py-1 text-xs font-bold text-white">
              Featured
            </span>
          )}
          {!product.inStock && (
            <span className="absolute right-3 top-3 rounded-full bg-zinc-900/80 px-2.5 py-1 text-xs font-semibold text-white">
              Sold out
            </span>
          )}
        </div>
        <div className="p-4">
          <p className="text-xs font-medium uppercase tracking-wide text-zinc-400">
            {product.brand ?? product.category}
          </p>
          <h3 className="mt-0.5 truncate font-semibold">{product.name}</h3>
          <div className="mt-1.5 flex items-center justify-between">
            <Price value={product.price} className="text-brand-600 dark:text-brand-400" />
            {product.reviewCount > 0 && <Rating value={product.avgRating} size="text-xs" />}
          </div>
        </div>
      </Link>
    </motion.div>
  );
}
