export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: "Customer" | "Admin";
  avatarUrl: string | null;
  phone: string | null;
  emailVerified: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

export interface ProductListItem {
  id: string;
  name: string;
  slug: string;
  category: string;
  brand: string | null;
  price: number;
  currency: string;
  imageUrl: string | null;
  avgRating: number;
  reviewCount: number;
  isFeatured: boolean;
  inStock: boolean;
}

export interface Variant {
  id: string;
  sku: string;
  size: string | null;
  color: string | null;
  price: number;
  stock: number;
}

export interface ProductDetail {
  id: string;
  name: string;
  slug: string;
  description: string;
  category: string;
  categorySlug: string;
  brand: string | null;
  price: number;
  currency: string;
  avgRating: number;
  reviewCount: number;
  isFeatured: boolean;
  images: { id: string; url: string; altText: string; isPrimary: boolean }[];
  variants: Variant[];
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  children: Category[];
}

export interface Brand {
  id: string;
  name: string;
  slug: string;
}

export interface CartItem {
  id: string;
  variantId: string;
  productId: string;
  productName: string;
  productSlug: string;
  variantLabel: string;
  imageUrl: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  stockAvailable: number;
}

export interface Cart {
  id: string;
  items: CartItem[];
  subtotal: number;
  itemCount: number;
}

export interface WishlistItem {
  productId: string;
  name: string;
  slug: string;
  price: number;
  imageUrl: string | null;
  avgRating: number;
  inStock: boolean;
}

export interface Quote {
  subtotal: number;
  discountAmount: number;
  shippingFee: number;
  total: number;
  couponCode: string | null;
  couponError: string | null;
}

export interface Address {
  id: string;
  label: string;
  fullName: string;
  phone: string;
  line1: string;
  city: string;
  county: string;
  isDefault: boolean;
}

export interface OrderItem {
  productName: string;
  variantLabel: string;
  imageUrl: string | null;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  id: string;
  orderNumber: string;
  status: string;
  subtotal: number;
  discountAmount: number;
  shippingFee: number;
  total: number;
  currency: string;
  couponCode: string | null;
  paymentProvider: string;
  paymentStatus: string;
  shippingAddress: { fullName: string; phone: string; line1: string; city: string; county: string };
  items: OrderItem[];
  trackingNumber: string | null;
  placedAt: string;
  canBeCancelled: boolean;
  timeline: { status: string; at: string | null }[];
}

export interface OrderListItem {
  id: string;
  orderNumber: string;
  status: string;
  total: number;
  currency: string;
  itemCount: number;
  firstImageUrl: string | null;
  placedAt: string;
}

export interface Review {
  id: string;
  userId: string;
  userName: string;
  rating: number;
  title: string;
  body: string;
  isVerifiedPurchase: boolean;
  createdAt: string;
}

export interface Notification {
  id: string;
  type: string;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}
