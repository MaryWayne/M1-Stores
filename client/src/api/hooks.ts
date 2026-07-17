import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { del, get, post, put } from "./client";
import type {
  Address, AuthResponse, Cart, Category, Notification, Order, OrderListItem,
  Paged, ProductDetail, ProductListItem, Quote, Review, User, WishlistItem,
} from "./types";
import { useAuth } from "../stores/auth";

// ---- Catalog ----

export interface ShopFilters {
  search?: string;
  categorySlug?: string;
  brandSlug?: string;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  inStock?: boolean;
  featured?: boolean;
  sort?: string;
  page?: number;
  pageSize?: number;
}

const qs = (params: Record<string, unknown>) => {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params))
    if (value !== undefined && value !== null && value !== "") search.set(key, String(value));
  const s = search.toString();
  return s ? `?${s}` : "";
};

export const useProducts = (filters: ShopFilters) =>
  useQuery({
    queryKey: ["products", filters],
    queryFn: () => get<Paged<ProductListItem>>(`/products${qs({ ...filters })}`),
    placeholderData: (prev) => prev,
  });

export const useProduct = (slug: string) =>
  useQuery({ queryKey: ["product", slug], queryFn: () => get<ProductDetail>(`/products/${slug}`) });

export const useCategories = () =>
  useQuery({ queryKey: ["categories"], queryFn: () => get<Category[]>("/categories"), staleTime: Infinity });

export const useReviews = (productId: string | undefined) =>
  useQuery({
    queryKey: ["reviews", productId],
    queryFn: () => get<Paged<Review>>(`/products/${productId}/reviews`),
    enabled: !!productId,
  });

// ---- Cart & wishlist ----

const useAuthed = () => !!useAuth((s) => s.accessToken);

export const useCart = () =>
  useQuery({ queryKey: ["cart"], queryFn: () => get<Cart>("/cart"), enabled: useAuthed() });

export function useCartMutations() {
  const qc = useQueryClient();
  const invalidate = (cart: Cart) => qc.setQueryData(["cart"], cart);
  const add = useMutation({
    mutationFn: (v: { variantId: string; quantity: number }) => post<Cart>("/cart/items", v),
    onSuccess: invalidate,
  });
  const update = useMutation({
    mutationFn: (v: { itemId: string; quantity: number }) =>
      put<Cart>(`/cart/items/${v.itemId}`, { quantity: v.quantity }),
    onSuccess: invalidate,
  });
  const remove = useMutation({
    mutationFn: (itemId: string) => del<Cart>(`/cart/items/${itemId}`),
    onSuccess: invalidate,
  });
  return { add, update, remove };
}

export const useWishlist = () =>
  useQuery({ queryKey: ["wishlist"], queryFn: () => get<WishlistItem[]>("/wishlist"), enabled: useAuthed() });

export function useWishlistMutations() {
  const qc = useQueryClient();
  const refetch = () => qc.invalidateQueries({ queryKey: ["wishlist"] });
  const add = useMutation({ mutationFn: (productId: string) => post(`/wishlist/${productId}`), onSuccess: refetch });
  const remove = useMutation({ mutationFn: (productId: string) => del(`/wishlist/${productId}`), onSuccess: refetch });
  return { add, remove };
}

// ---- Checkout & orders ----

export const useQuote = (couponCode: string | null, enabled: boolean) =>
  useQuery({
    queryKey: ["quote", couponCode],
    queryFn: () => post<Quote>("/checkout/quote", { couponCode }),
    enabled,
  });

export const useCheckout = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { addressId: string; couponCode: string | null; paymentProvider: number; mpesaPhone: string | null }) =>
      post<{ order: Order; redirectUrl: string | null }>("/checkout", v),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["cart"] });
      qc.invalidateQueries({ queryKey: ["orders"] });
    },
  });
};

export const useOrders = (page: number) =>
  useQuery({
    queryKey: ["orders", page],
    queryFn: () => get<Paged<OrderListItem>>(`/orders?page=${page}&pageSize=10`),
    enabled: useAuthed(),
  });

export const useOrder = (orderNumber: string | undefined) =>
  useQuery({
    queryKey: ["order", orderNumber],
    queryFn: () => get<Order>(`/orders/${orderNumber}`),
    enabled: !!orderNumber,
  });

export const useCancelOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (orderNumber: string) => post<Order>(`/orders/${orderNumber}/cancel`),
    onSuccess: (order) => {
      qc.setQueryData(["order", order.orderNumber], order);
      qc.invalidateQueries({ queryKey: ["orders"] });
    },
  });
};

// ---- Addresses ----

export const useAddresses = () =>
  useQuery({ queryKey: ["addresses"], queryFn: () => get<Address[]>("/addresses"), enabled: useAuthed() });

export function useAddressMutations() {
  const qc = useQueryClient();
  const refetch = () => qc.invalidateQueries({ queryKey: ["addresses"] });
  const save = useMutation({
    mutationFn: (v: { id?: string; body: Omit<Address, "id"> }) =>
      v.id ? put<Address>(`/addresses/${v.id}`, v.body) : post<Address>("/addresses", v.body),
    onSuccess: refetch,
  });
  const remove = useMutation({ mutationFn: (id: string) => del(`/addresses/${id}`), onSuccess: refetch });
  return { save, remove };
}

// ---- Notifications ----

export const useNotifications = () =>
  useQuery({
    queryKey: ["notifications"],
    queryFn: () => get<Paged<Notification>>("/notifications?pageSize=20"),
    enabled: useAuthed(),
    refetchInterval: 60_000,
  });

export const useMarkNotificationsRead = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (ids: string[] | null) => put("/notifications/read", ids),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["notifications"] }),
  });
};

// ---- Auth ----

export function useAuthMutations() {
  const setAuth = useAuth((s) => s.setAuth);
  const login = useMutation({
    mutationFn: (v: { email: string; password: string }) => post<AuthResponse>("/auth/login", v),
    onSuccess: setAuth,
  });
  const register = useMutation({
    mutationFn: (v: { email: string; password: string; fullName: string }) =>
      post<AuthResponse>("/auth/register", v),
    onSuccess: setAuth,
  });
  return { login, register };
}

export const useUpdateProfile = () => {
  const setUser = useAuth((s) => s.setUser);
  return useMutation({
    mutationFn: (v: { fullName?: string; phone?: string; currentPassword?: string; newPassword?: string }) =>
      put<User>("/auth/me", v),
    onSuccess: setUser,
  });
};

export const useCreateReview = (productId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { rating: number; title: string; body: string }) =>
      post<Review>(`/products/${productId}/reviews`, v),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["reviews", productId] });
      qc.invalidateQueries({ queryKey: ["product"] });
    },
  });
};
