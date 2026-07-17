import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useAuthMutations } from "../api/hooks";
import { post } from "../api/client";
import { ApiError } from "../api/client";
import { useToasts } from "../stores/toast";

function AuthShell({ title, subtitle, children }: { title: string; subtitle?: string; children: React.ReactNode }) {
  return (
    <div className="mx-auto max-w-md py-10">
      <div className="rounded-3xl border border-zinc-200 p-8 shadow-sm dark:border-zinc-800">
        <h1 className="text-2xl font-extrabold">{title}</h1>
        {subtitle && <p className="mt-1 text-sm text-zinc-500">{subtitle}</p>}
        <div className="mt-6">{children}</div>
      </div>
    </div>
  );
}

const inputCls = "w-full rounded-xl border border-zinc-200 bg-transparent px-4 py-2.5 text-sm outline-none transition focus:border-brand-400 dark:border-zinc-700";
const buttonCls = "w-full rounded-full bg-gradient-to-r from-brand-500 to-accent-500 py-3 font-bold text-white transition hover:opacity-90 disabled:opacity-40";

export function LoginPage() {
  const { login } = useAuthMutations();
  const navigate = useNavigate();
  const toast = useToasts((s) => s.push);
  const [email, setEmail] = useState("demo@m1stores.com");
  const [password, setPassword] = useState("Demo!2026");
  const [error, setError] = useState("");

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    try {
      const auth = await login.mutateAsync({ email, password });
      toast(`Welcome back, ${auth.user.fullName.split(" ")[0]}! 👋`);
      navigate(auth.user.role === "Admin" ? "/admin" : "/");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Sign in failed");
    }
  };

  return (
    <AuthShell title="Welcome back" subtitle="Demo account is pre-filled — just press sign in.">
      <form onSubmit={submit} className="space-y-3">
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email" required className={inputCls} />
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Password" required className={inputCls} />
        {error && <p className="text-sm text-rose-500">{error}</p>}
        <button type="submit" disabled={login.isPending} className={buttonCls}>
          {login.isPending ? "Signing in…" : "Sign in"}
        </button>
      </form>
      <div className="mt-4 flex items-center justify-between text-sm">
        <Link to="/forgot-password" className="text-zinc-500 hover:text-brand-500">Forgot password?</Link>
        <Link to="/register" className="font-semibold text-brand-500">Create account</Link>
      </div>
      <p className="mt-4 rounded-xl bg-zinc-50 p-3 text-xs text-zinc-500 dark:bg-zinc-900">
        Try the admin side: <span className="font-mono">admin@m1stores.com</span> / <span className="font-mono">Admin!2026</span>
      </p>
    </AuthShell>
  );
}

export function RegisterPage() {
  const { register } = useAuthMutations();
  const navigate = useNavigate();
  const toast = useToasts((s) => s.push);
  const [form, setForm] = useState({ fullName: "", email: "", password: "" });
  const [error, setError] = useState("");

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    try {
      await register.mutateAsync(form);
      toast("Account created — welcome to M1 Stores! 🎉");
      navigate("/");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Registration failed");
    }
  };

  return (
    <AuthShell title="Create your account" subtitle="Get 10% off your first order with code WELCOME10.">
      <form onSubmit={submit} className="space-y-3">
        <input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} placeholder="Full name" required className={inputCls} />
        <input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} placeholder="Email" required className={inputCls} />
        <input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} placeholder="Password (8+ chars, letters & numbers)" required className={inputCls} />
        {error && <p className="text-sm text-rose-500">{error}</p>}
        <button type="submit" disabled={register.isPending} className={buttonCls}>
          {register.isPending ? "Creating…" : "Create account"}
        </button>
      </form>
      <p className="mt-4 text-center text-sm text-zinc-500">
        Already have an account? <Link to="/login" className="font-semibold text-brand-500">Sign in</Link>
      </p>
    </AuthShell>
  );
}

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    await post("/auth/forgot-password", { email });
    setSent(true);
  };

  return (
    <AuthShell title="Reset your password">
      {sent ? (
        <p className="text-sm text-zinc-600 dark:text-zinc-400">
          If an account exists for <strong>{email}</strong>, a reset link is on its way. Check your inbox.
        </p>
      ) : (
        <form onSubmit={submit} className="space-y-3">
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Your account email" required className={inputCls} />
          <button type="submit" className={buttonCls}>Send reset link</button>
        </form>
      )}
    </AuthShell>
  );
}

export function ResetPasswordPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const toast = useToasts((s) => s.push);
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await post("/auth/reset-password", { token: params.get("token"), newPassword: password });
      toast("Password updated — sign in with your new password");
      navigate("/login");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Reset failed");
    }
  };

  return (
    <AuthShell title="Choose a new password">
      <form onSubmit={submit} className="space-y-3">
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="New password" required className={inputCls} />
        {error && <p className="text-sm text-rose-500">{error}</p>}
        <button type="submit" className={buttonCls}>Update password</button>
      </form>
    </AuthShell>
  );
}

export function VerifyEmailPage() {
  const [params] = useSearchParams();
  const [state, setState] = useState<"idle" | "ok" | "fail">("idle");

  const verify = async () => {
    try {
      await post("/auth/verify-email", { token: params.get("token") });
      setState("ok");
    } catch {
      setState("fail");
    }
  };

  return (
    <AuthShell title="Verify your email">
      {state === "idle" && (
        <button onClick={verify} className={buttonCls}>Verify my email</button>
      )}
      {state === "ok" && <p className="text-sm text-emerald-600">✅ Email verified — you're all set!</p>}
      {state === "fail" && <p className="text-sm text-rose-500">This link is invalid or expired. Request a new one from your account page.</p>}
    </AuthShell>
  );
}
