import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { del, get, post, put } from "./client";
import type { Order, Paged, ProductDetail } from "./types";

export interface Dashboard {
  totalRevenue: number;
  totalOrders: number;
  pendingOrders: number;
  totalCustomers: number;
  avgOrderValue: number;
  topProducts: { name: string; unitsSold: number; revenue: number }[];
  lowStock: { variantId: string; product: string; variant: string; sku: string; stock: number }[];
  recentOrders: { id: string; orderNumber: string; status: string; total: number; itemCount: number; placedAt: string }[];
}

export interface SalesPoint { period: string; revenue: number; orders: number }

export interface Customer {
  id: string; email: string; fullName: string; phone: string | null;
  emailVerified: boolean; isDeactivated: boolean; orders: number; totalSpent: number; joinedAt: string;
}

export interface Coupon {
  id: string; code: string; type: string; value: number; minOrderTotal: number | null;
  maxUses: number; usedCount: number; expiresAt: string | null; isActive: boolean;
}

export const useDashboard = () =>
  useQuery({ queryKey: ["admin", "dashboard"], queryFn: () => get<Dashboard>("/admin/dashboard") });

export const useSalesReport = () =>
  useQuery({ queryKey: ["admin", "sales"], queryFn: () => get<SalesPoint[]>("/admin/reports/sales?groupBy=day") });

export const useAdminOrders = (status: string, page: number) =>
  useQuery({
    queryKey: ["admin", "orders", status, page],
    queryFn: () => get<Paged<Order>>(`/admin/orders?page=${page}&pageSize=10${status ? `&status=${status}` : ""}`),
    placeholderData: (prev) => prev,
  });

export const useUpdateOrderStatus = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { orderId: string; status: string; trackingNumber: string | null }) =>
      put<Order>(`/admin/orders/${v.orderId}/status`, { status: v.status, trackingNumber: v.trackingNumber }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin"] }),
  });
};

export const useCustomers = (search: string, page: number) =>
  useQuery({
    queryKey: ["admin", "customers", search, page],
    queryFn: () => get<Paged<Customer>>(`/admin/customers?page=${page}&pageSize=10${search ? `&search=${encodeURIComponent(search)}` : ""}`),
    placeholderData: (prev) => prev,
  });

export const useSetCustomerActive = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { userId: string; active: boolean }) =>
      put(`/admin/customers/${v.userId}/active`, v.active),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin", "customers"] }),
  });
};

export const useCoupons = () =>
  useQuery({ queryKey: ["admin", "coupons"], queryFn: () => get<Coupon[]>("/admin/coupons") });

export const useSaveCoupon = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { id?: string; body: Omit<Coupon, "id" | "usedCount" | "type"> & { type: number } }) =>
      v.id ? put<Coupon>(`/admin/coupons/${v.id}`, v.body) : post<Coupon>("/admin/coupons", v.body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin", "coupons"] }),
  });
};

export const useDeleteCoupon = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => del(`/admin/coupons/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin", "coupons"] }),
  });
};

export interface SaveProductBody {
  name: string; description: string; categoryId: string; brandId: string | null;
  basePrice: number; isFeatured: boolean;
}

export const useBrands = () =>
  useQuery({ queryKey: ["brands"], queryFn: () => get<{ id: string; name: string; slug: string }[]>("/brands"), staleTime: Infinity });

export function useProductAdmin() {
  const qc = useQueryClient();
  const refetch = () => {
    qc.invalidateQueries({ queryKey: ["products"] });
    qc.invalidateQueries({ queryKey: ["product"] });
  };
  const save = useMutation({
    mutationFn: (v: { id?: string; body: SaveProductBody }) =>
      v.id ? put<ProductDetail>(`/admin/products/${v.id}`, v.body) : post<ProductDetail>("/admin/products", v.body),
    onSuccess: refetch,
  });
  const remove = useMutation({
    mutationFn: (id: string) => del(`/admin/products/${id}`),
    onSuccess: refetch,
  });
  const updateVariant = useMutation({
    mutationFn: (v: { variantId: string; size: string | null; color: string | null; priceOverride: number | null; stock: number }) =>
      put(`/admin/variants/${v.variantId}`, { size: v.size, color: v.color, priceOverride: v.priceOverride, stock: v.stock }),
    onSuccess: refetch,
  });
  const addVariant = useMutation({
    mutationFn: (v: { productId: string; size: string | null; color: string | null; stock: number }) =>
      post(`/admin/products/${v.productId}/variants`, { size: v.size, color: v.color, priceOverride: null, stock: v.stock }),
    onSuccess: refetch,
  });
  return { save, remove, updateVariant, addVariant };
}
