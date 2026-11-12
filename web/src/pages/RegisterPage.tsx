import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { UserPlus } from "lucide-react";
import { useAuth } from "../api/AuthContext";
import type { ApiError } from "../api/client";
import { Field } from "../components/Field";

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ username: "", email: "", password: "", bio: "" });
  const [agreed, setAgreed] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await register({ ...form, username: form.username.trim(), email: form.email.trim(), bio: form.bio.trim() || undefined });
      navigate("/me", { replace: true });
    } catch (err) {
      setError(err as ApiError);
    } finally {
      setBusy(false);
    }
  }

  // 409 conflicts name the field in their title; validation errors come per field.
  const conflict = error?.status === 409 ? error.problem.title : undefined;
  const usernameError = error?.fieldError("username") ?? (conflict?.startsWith("Username") ? error!.problem.detail : undefined);
  const emailError = error?.fieldError("email") ?? (conflict?.startsWith("Email") ? error!.problem.detail : undefined);

  return (
    <div className="container page page--narrow">
      <div className="auth-card">
        <h1>Create an account</h1>
        <p className="muted">Your username is shown next to every review you write.</p>
        <form onSubmit={submit} className="stack" noValidate>
          <Field
            id="username"
            label="Username"
            autoComplete="username"
            required
            minLength={3}
            maxLength={50}
            pattern="[A-Za-z0-9_.\-]+"
            help="3 to 50 letters, digits, _ . or -"
            error={usernameError}
            value={form.username}
            onChange={(e) => setForm({ ...form, username: e.target.value })}
          />
          <Field
            id="email"
            label="Email"
            type="email"
            autoComplete="email"
            required
            help="Only you and admins can see it."
            error={emailError}
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
          />
          <Field
            id="password"
            label="Password"
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            help="At least 8 characters."
            error={error?.fieldError("password")}
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
          />
          <div className="field">
            <label htmlFor="bio">About you (optional)</label>
            <textarea
              id="bio"
              rows={3}
              maxLength={500}
              placeholder="What do you listen to?"
              value={form.bio}
              onChange={(e) => setForm({ ...form, bio: e.target.value })}
            />
          </div>
          <label className="checkbox">
            <input type="checkbox" checked={agreed} onChange={(e) => setAgreed(e.target.checked)} required />
            I understand my reviews and username are public.
          </label>
          {error && !usernameError && !emailError && !error.fieldError("password") && (
            <p className="form-error" role="alert">
              {error.problem.detail ?? error.problem.title}
            </p>
          )}
          <button
            className="btn btn--primary btn--block"
            type="submit"
            disabled={busy || !agreed || !form.username || !form.email || form.password.length < 8}
          >
            <UserPlus size={18} aria-hidden="true" /> {busy ? "Creating account…" : "Create account"}
          </button>
        </form>
        <p className="auth-card__switch">
          Already have an account? <Link to="/login">Log in</Link>
        </p>
      </div>
    </div>
  );
}
