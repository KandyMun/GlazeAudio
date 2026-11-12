import { useState, type FormEvent } from "react";
import { Link, Navigate, useNavigate, useParams } from "react-router-dom";
import { Calendar, KeyRound, MessageSquare, Save, Trash2 } from "lucide-react";
import { useAuth } from "../api/AuthContext";
import { api, ApiError, query } from "../api/client";
import type { Paged, User, UserReview } from "../api/types";
import { ConfirmModal } from "../components/ConfirmModal";
import { Field } from "../components/Field";
import { formatDate, plural } from "../components/format";
import { ErrorNotice, Loading } from "../components/Notice";
import { Pagination } from "../components/Pagination";
import { Rating } from "../components/Rating";
import { useToast } from "../components/Toast";
import { useApi } from "../components/useApi";

/** /me (your own profile, editable) and /users/:userId (anyone's public profile). */
export function ProfilePage() {
  const params = useParams();
  const { user: me, setUser, isAdmin, logout } = useAuth();
  const navigate = useNavigate();
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [deleting, setDeleting] = useState(false);

  const userId = params.userId ? Number(params.userId) : me?.id;
  const isOwn = me !== null && userId === me.id;

  const profile = useApi<User>(userId ? `/api/users/${userId}` : null);
  const reviews = useApi<Paged<UserReview>>(userId ? `/api/users/${userId}/reviews${query({ page, pageSize: 5 })}` : null);

  if (!params.userId && !me) return <Navigate to="/login" state={{ from: "/me" }} replace />;
  if (profile.error) return <div className="container page"><ErrorNotice error={profile.error} onRetry={profile.reload} /></div>;
  if (!profile.data) return <div className="container page"><Loading label="Loading profile" /></div>;

  const u = profile.data;

  return (
    <div className="container page">
      <section className="profile-head">
        <div className="avatar" aria-hidden="true">
          {u.username.slice(0, 1).toUpperCase()}
        </div>
        <div>
          <h1>
            {u.username} {u.role === "Admin" && <span className="tag tag--admin">Admin</span>}
          </h1>
          {u.bio && <p className="profile-head__bio">{u.bio}</p>}
          <ul className="facts">
            <li>
              <MessageSquare size={18} aria-hidden="true" /> {plural(u.reviewCount, "review")}
            </li>
            <li>
              <Calendar size={18} aria-hidden="true" /> Joined {formatDate(u.createdAt)}
            </li>
          </ul>
        </div>
      </section>

      <div className="profile-columns">
        <section aria-labelledby="reviews-title">
          <h2 id="reviews-title">{isOwn ? "Your reviews" : "Reviews"}</h2>
          {reviews.data && reviews.data.items.length === 0 && (
            <div className="empty">
              <p>{isOwn ? "You haven't reviewed anything yet." : "No reviews yet."}</p>
              {isOwn && (
                <Link className="btn btn--primary" to="/">
                  Find an album to review
                </Link>
              )}
            </div>
          )}
          <ul className="review-snippets">
            {reviews.data?.items.map((r) => (
              <li key={r.id} className="review-snippet">
                <p className="review-snippet__head">
                  <Rating value={r.overallRating} label="Overall rating" />
                  <Link to={`/albums/${r.albumId}/songs/${r.songId}`}>{r.songTitle}</Link>
                </p>
                <p className="muted">
                  {r.albumArtist}, <Link to={`/albums/${r.albumId}`}>{r.albumTitle}</Link>
                </p>
                <blockquote>{r.comment}</blockquote>
                <p className="muted review-snippet__by">{formatDate(r.createdAt)}</p>
              </li>
            ))}
          </ul>
          {reviews.data && <Pagination page={reviews.data.page} totalPages={reviews.data.totalPages} onChange={setPage} />}
        </section>

        {isOwn && (
          <aside className="profile-settings">
            <AccountForm user={u} onSaved={(saved) => { setUser(saved); profile.reload(); }} />
            <PasswordForm />
            <section className="panel panel--danger stack" aria-labelledby="danger-title">
              <h2 id="danger-title">Delete account</h2>
              <p>Your account and all {plural(u.reviewCount, "review")} you wrote will be removed for good.</p>
              <button className="btn btn--danger" onClick={() => setDeleting(true)}>
                <Trash2 size={18} aria-hidden="true" /> Delete my account
              </button>
            </section>
          </aside>
        )}
        {!isOwn && isAdmin && (
          <aside className="profile-settings">
            <section className="panel panel--danger stack" aria-labelledby="danger-title">
              <h2 id="danger-title">Admin</h2>
              <p>Removing this user also removes their {plural(u.reviewCount, "review")}.</p>
              <button className="btn btn--danger" onClick={() => setDeleting(true)}>
                <Trash2 size={18} aria-hidden="true" /> Delete user
              </button>
            </section>
          </aside>
        )}
      </div>

      <ConfirmModal
        open={deleting}
        title={isOwn ? "Delete your account?" : `Delete ${u.username}?`}
        confirmLabel={isOwn ? "Delete my account" : "Delete user"}
        cancelLabel="Keep it"
        typeToConfirm={u.username}
        onClose={() => setDeleting(false)}
        onConfirm={async () => {
          await api(`/api/users/${u.id}`, { method: "DELETE" });
          setDeleting(false);
          if (isOwn) {
            await logout();
            toast("Your account was deleted.");
          } else {
            toast(`${u.username} was deleted.`);
          }
          navigate("/");
        }}
      >
        <p>
          {isOwn ? "Your account" : <>The account <strong>{u.username}</strong></>} and {plural(u.reviewCount, "review")} will be
          deleted. This can't be undone.
        </p>
      </ConfirmModal>
    </div>
  );
}

function AccountForm({ user, onSaved }: { user: User; onSaved(user: User): void }) {
  const [form, setForm] = useState({ username: user.username, email: user.email ?? "", bio: user.bio ?? "" });
  const [error, setError] = useState<ApiError | null>(null);
  const [saved, setSaved] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSaved(false);
    try {
      onSaved(await api<User>(`/api/users/${user.id}`, { method: "PUT", body: form }));
      setSaved(true);
    } catch (err) {
      setError(err as ApiError);
    }
  }

  return (
    <form className="panel stack" onSubmit={submit} noValidate>
      <h2>Account</h2>
      <Field id="acc-username" label="Username" value={form.username} error={error?.fieldError("username")} onChange={(e) => setForm({ ...form, username: e.target.value })} />
      <Field id="acc-email" label="Email" type="email" value={form.email} error={error?.fieldError("email")} onChange={(e) => setForm({ ...form, email: e.target.value })} />
      <div className="field">
        <label htmlFor="acc-bio">About you</label>
        <textarea id="acc-bio" rows={3} maxLength={500} value={form.bio} onChange={(e) => setForm({ ...form, bio: e.target.value })} />
      </div>
      {error && !error.problem.errors && <p className="form-error" role="alert">{error.problem.detail ?? error.problem.title}</p>}
      {saved && <p className="form-success" role="status">Changes saved.</p>}
      <button className="btn btn--primary" type="submit">
        <Save size={18} aria-hidden="true" /> Save changes
      </button>
    </form>
  );
}

function PasswordForm() {
  const [form, setForm] = useState({ currentPassword: "", newPassword: "" });
  const [error, setError] = useState<ApiError | null>(null);
  const [done, setDone] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setDone(false);
    try {
      await api("/api/auth/password", { method: "PUT", body: form });
      setForm({ currentPassword: "", newPassword: "" });
      setDone(true);
    } catch (err) {
      setError(err as ApiError);
    }
  }

  return (
    <form className="panel stack" onSubmit={submit} noValidate>
      <h2>Password</h2>
      <Field id="pw-current" label="Current password" type="password" autoComplete="current-password" value={form.currentPassword} error={error?.fieldError("currentPassword")} onChange={(e) => setForm({ ...form, currentPassword: e.target.value })} />
      <Field id="pw-new" label="New password" type="password" autoComplete="new-password" minLength={8} help="At least 8 characters. Other devices will be logged out." value={form.newPassword} error={error?.fieldError("newPassword")} onChange={(e) => setForm({ ...form, newPassword: e.target.value })} />
      {done && <p className="form-success" role="status">Password changed.</p>}
      <button className="btn btn--ghost" type="submit" disabled={!form.currentPassword || form.newPassword.length < 8}>
        <KeyRound size={18} aria-hidden="true" /> Change password
      </button>
    </form>
  );
}
