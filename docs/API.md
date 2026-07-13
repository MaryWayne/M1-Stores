# M1 Stores — REST API Reference

Base URL: `/api/v1`. All responses JSON. Errors follow RFC 7807 `ProblemDetails`.
Auth: `Authorization: Bearer <jwt>`. Full interactive docs at `/swagger`.

Conventions: list endpoints support `?page=1&pageSize=20`, return
`{ items, page, pageSize, totalCount, totalPages }`.

## Auth — `/auth`
| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/auth/register` | — | Register; sends verification email |
| POST | `/auth/login` | — | Email+password → access + refresh tokens |
| POST | `/auth/google` | — | Google ID token → app tokens (creates account on first login) |
| POST | `/auth/refresh` | — | Rotate refresh token |
| POST | `/auth/logout` | ✓ | Revoke refresh token |
| POST | `/auth/verify-email` | — | Consume email verification token |
| POST | `/auth/resend-verification` | — | Re-send verification email |
| POST | `/auth/forgot-password` | — | Send reset email (uniform response) |
| POST | `/auth/reset-password` | — | Consume reset token, set new password |
| GET  | `/auth/me` | ✓ | Current user profile |
| PUT  | `/auth/me` | ✓ | Update profile / change password |

## Catalog — `/catalog` (public)
| Method | Route | Description |
|---|---|---|
| GET | `/products` | Search & browse. Query: `search`, `categorySlug`, `brandSlug`, `minPrice`, `maxPrice`, `minRating`, `inStock`, `sort` (`newest`/`price-asc`/`price-desc`/`rating`/`popular`), paging |
| GET | `/products/{slug}` | Product detail with variants, images, rating summary |
| GET | `/products/compare?ids=a,b,c` | Side-by-side comparison (max 4) |
| GET | `/products/{id}/reviews` | Paged reviews |
| GET | `/categories` | Category tree |
| GET | `/brands` | Brand list |

## Cart & wishlist — auth required
| Method | Route | Description |
|---|---|---|
| GET | `/cart` | Current cart with totals |
| POST | `/cart/items` | Add variant `{variantId, quantity}` |
| PUT | `/cart/items/{id}` | Change quantity |
| DELETE | `/cart/items/{id}` | Remove line |
| POST | `/cart/merge` | Merge guest cart on login |
| GET/POST/DELETE | `/wishlist`, `/wishlist/{productId}` | Wishlist CRUD |

## Checkout & orders — auth required
| Method | Route | Description |
|---|---|---|
| POST | `/checkout/quote` | Validate cart + coupon → totals & shipping fee |
| POST | `/checkout` | Place order `{addressId, couponCode?, paymentProvider}` |
| POST | `/payments/mpesa/initiate` | STK push (Daraja sandbox) for an order |
| POST | `/payments/mpesa/callback` | Daraja webhook (anonymous, signature-checked) |
| POST | `/payments/stripe/checkout-session` | Stripe test-mode session for an order |
| POST | `/payments/stripe/webhook` | Stripe webhook |
| GET | `/orders` | My orders, paged |
| GET | `/orders/{orderNumber}` | Detail + tracking timeline |
| POST | `/orders/{orderNumber}/cancel` | Cancel while `PendingPayment`/`Paid` |
| GET | `/orders/{orderNumber}/invoice` | PDF invoice |

## Reviews — auth required
| POST | `/products/{id}/reviews` | Create (verified-purchase gate) |
| PUT/DELETE | `/reviews/{id}` | Edit/remove own review |

## Notifications — auth required
| GET | `/notifications` | Paged, newest first |
| PUT | `/notifications/read` | Mark all/selected read |

## Admin — `/admin`, role `Admin`
| Method | Route | Description |
|---|---|---|
| GET | `/admin/dashboard` | KPIs: revenue, orders, AOV, top products, low stock |
| GET | `/admin/reports/sales?from&to&groupBy` | Revenue series for charts; CSV export |
| CRUD | `/admin/products`, `/admin/products/{id}/images`, `/admin/variants` | Catalog & inventory management (image upload = multipart) |
| CRUD | `/admin/categories`, `/admin/brands` | Taxonomy |
| GET/PUT | `/admin/orders`, `/admin/orders/{id}/status` | Order management, fulfilment, shipment tracking numbers |
| GET/PUT | `/admin/customers`, `/admin/customers/{id}` | Customer list, deactivate |
| CRUD | `/admin/coupons` | Discounts & coupons |
| GET/PUT | `/admin/settings` | Store settings (name, currency, shipping fees, feature flags) |

## System
| GET | `/health` | Liveness + DB check |
