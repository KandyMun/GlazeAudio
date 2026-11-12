import { useState, type FormEvent } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { LogIn } from "lucide-react";
import { useAuth } from "../api/AuthContext";
import type { ApiError } from "../api/client";
import { Field } from "../components/Field";

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const from = (useLocation().state as { from?: string } | null)?.from ?? "/";
  const [form, setForm] = useState({ login: "", password: "" });
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await login(form.login.trim(), form.password);
      navigate(from, { replace: true });
    } catch (err) {
      setError(err as ApiError);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="container page page--narrow">
      <div className="auth-card">
        <h1>Log in</h1>
        <p className="muted">Welcome back. Log in to rate songs and manage your reviews.</p>
        <form onSubmit={submit} className="stack" noValidate>
          <Field
            id="login"
            label="Username or email"
            autoComplete="username"
            required
            value={form.login}
            onChange={(e) => setForm({ ...form, login: e.target.value })}
          />
          <Field
            id="password"
            label="Password"
            type={showPassword ? "text" : "password"}
            autoComplete="current-password"
            required
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
          />
          <label className="checkbox">
            <input type="checkbox" checked={showPassword} onChange={(e) => setShowPassword(e.target.checked)} />
            Show password
          </label>
          {error && (
            <p className="form-error" role="alert">
              {error.status === 401 ? "That username or password doesn't match an account." : error.problem.detail ?? error.problem.title}
            </p>
          )}
          <button className="btn btn--primary btn--block" type="submit" disabled={busy || !form.login || !form.password}>
            <LogIn size={18} aria-hidden="true" /> {busy ? "Logging in…" : "Log in"}
          </button>
        </form>
        <p className="auth-card__switch">
          New here? <Link to="/register">Create an account</Link>
        </p>
      </div>
    </div>
  );
}
