# M1 Stores — Deployment Guide

Total cost: **KES 0/month**. Total time: ~15 minutes. Three free accounts are
needed (all support "Sign up with GitHub"): [Neon](https://neon.tech),
[Render](https://render.com), [Vercel](https://vercel.com).

## 1. Database — Neon (PostgreSQL)

1. Sign up at neon.tech with GitHub → create project `m1-stores` (region: closest to your users).
2. Copy the **connection string** (looks like `postgresql://user:pass@…/neondb?sslmode=require`).
3. Convert to the .NET format used by `ConnectionStrings__Default`:
   `Host=<host>;Database=neondb;Username=<user>;Password=<pass>;SSL Mode=Require`

## 2. API — Render

1. Sign up at render.com with GitHub → **New → Blueprint** → pick `MaryWayne/M1-Stores`.
   Render reads [render.yaml](../render.yaml) automatically.
2. When prompted, paste:
   - `ConnectionStrings__Default` → the Neon string from step 1
   - `App__FrontendUrl` and `Cors__Origins__0` → your Vercel URL (add after step 3, then redeploy)
3. Deploy. First boot runs EF migrations and seeds the demo catalog automatically.
4. Note your API URL, e.g. `https://m1-stores-api.onrender.com` — Swagger lives at `/swagger`.

> Free-tier note: the service sleeps after 15 idle minutes; the first request
> after that takes ~1 minute. Normal for demos.

## 3. Frontend — Vercel

1. Sign up at vercel.com with GitHub → **Add New → Project** → import `MaryWayne/M1-Stores`.
2. Set **Root Directory** = `client`. Framework preset: Vite.
3. Add environment variable: `VITE_API_URL` = your Render API URL (no trailing slash).
4. Deploy → you get `https://<project>.vercel.app`.
5. Because it's a SPA with client routing, add [client/vercel.json](../client/vercel.json)
   (already in the repo) — it rewrites all routes to `index.html`.
6. Go back to Render and fill `App__FrontendUrl` + `Cors__Origins__0` with this URL, redeploy.

## 4. Optional integrations (config-gated — the app works without them)

| Feature | Env vars | Where to get them |
|---|---|---|
| Real emails | `Email__Host/Port/User/Password/From` | brevo.com free tier (300/day) |
| Google login | `Google__ClientId` | Google Cloud Console OAuth client |
| M-Pesa sandbox | `Mpesa__ConsumerKey/ConsumerSecret/ShortCode/Passkey/CallbackUrl` | developer.safaricom.co.ke |
| Stripe test | `Stripe__SecretKey` | stripe.com test keys |
| Admin login | `Seed__AdminEmail` / `Seed__AdminPassword` | your choice (set before first boot) |

## Smoke test after deploy

1. `https://<api>/health` → `Healthy`
2. `https://<api>/swagger` → interactive docs
3. Storefront loads products, demo login works, demo checkout completes.
