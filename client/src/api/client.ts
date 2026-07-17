import { useAuth } from "../stores/auth";
import type { AuthResponse } from "./types";

// Production falls back to the Render service named in render.yaml;
// set VITE_API_URL in Vercel to override.
const BASE =
  (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, "") ??
  (import.meta.env.PROD ? "https://m1-stores-api.onrender.com" : "");

export class ApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;
  constructor(status: number, message: string, errors?: Record<string, string[]>) {
    super(message);
    this.status = status;
    this.errors = errors;
  }
}

let refreshing: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  const { refreshToken, setAuth, logout } = useAuth.getState();
  if (!refreshToken) return false;
  refreshing ??= (async () => {
    try {
      const res = await fetch(`${BASE}/api/v1/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken }),
      });
      if (!res.ok) throw new Error("refresh failed");
      setAuth((await res.json()) as AuthResponse);
      return true;
    } catch {
      logout();
      return false;
    } finally {
      refreshing = null;
    }
  })();
  return refreshing;
}

export async function api<T>(
  method: string,
  path: string,
  body?: unknown,
  retried = false,
): Promise<T> {
  const token = useAuth.getState().accessToken;
  const res = await fetch(`${BASE}/api/v1${path}`, {
    method,
    headers: {
      ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (res.status === 401 && !retried && useAuth.getState().refreshToken) {
    if (await tryRefresh()) return api<T>(method, path, body, true);
  }

  if (!res.ok) {
    let message = "Something went wrong.";
    let errors: Record<string, string[]> | undefined;
    try {
      const problem = await res.json();
      message = problem.title ?? message;
      errors = problem.errors;
      if (errors) message = Object.values(errors).flat()[0] ?? message;
    } catch { /* non-JSON error body */ }
    throw new ApiError(res.status, message, errors);
  }

  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const get = <T>(path: string) => api<T>("GET", path);
export const post = <T>(path: string, body?: unknown) => api<T>("POST", path, body);
export const put = <T>(path: string, body?: unknown) => api<T>("PUT", path, body);
export const del = <T>(path: string) => api<T>("DELETE", path);
