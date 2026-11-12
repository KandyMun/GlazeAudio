import { useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { ArrowLeft, Clock, LogIn, Pencil, Trash2 } from "lucide-react";
import { useAuth } from "../api/AuthContext";
import { api, ApiError, query } from "../api/client";
import type { Album, Paged, Review, Song } from "../api/types";
import { formatDuration, plural } from "../components/format";
import { Modal } from "../components/Modal";
import { ErrorNotice, Loading } from "../components/Notice";
import { Pagination } from "../components/Pagination";
import { Rating } from "../components/Rating";
import { ReviewCard } from "../components/ReviewCard";
import { ReviewForm, type ReviewInput } from "../components/ReviewForm";
import { useApi } from "../components/useApi";

export function SongPage() {
  const { albumId, songId } = useParams();
  const { user, isAdmin } = useAuth();
  const [params, setParams] = useSearchParams();
  const [editing, setEditing] = useState(false);
  const [toDelete, setToDelete] = useState<Review | null>(null);
  const [deleteError, setDeleteError] = useState<ApiError | null>(null);

  const base = `/api/albums/${albumId}/songs/${songId}`;
  const page = Number(params.get("page") ?? 1);
  const minRating = params.get("minRating") ?? "";

  const album = useApi<Album>(`/api/albums/${albumId}`);
  const song = useApi<Song>(base);
  const reviews = useApi<Paged<Review>>(`${base}/reviews${query({ page, pageSize: 5, minRating })}`);
  const mine = useApi<Paged<Review>>(user ? `${base}/reviews${query({ userId: user.id })}` : null);
  const myReview = mine.data?.items[0] ?? null;

  function refreshAll() {
    song.reload();
    reviews.reload();
    mine.reload();
  }

  async function create(value: ReviewInput) {
    await api(`${base}/reviews`, { method: "POST", body: value });
    refreshAll();
  }

  async function update(value: ReviewInput) {
    await api(`${base}/reviews/${myReview!.id}`, { method: "PUT", body: value });
    setEditing(false);
    refreshAll();
  }

  async function confirmDelete() {
    if (!toDelete) return;
    try {
      await api(`${base}/reviews/${toDelete.id}`, { method: "DELETE" });
      setToDelete(null);
      setEditing(false);
      refreshAll();
    } catch (err) {
      setDeleteError(err as ApiError);
    }
  }

  function setFilter(next: string) {
    const p = new URLSearchParams();
    if (next) p.set("minRating", next);
    setParams(p);
  }

  if (song.error) return <div className="container page"><ErrorNotice error={song.error} onRetry={song.reload} /></div>;
  if (!song.data) return <div className="container page"><Loading label="Loading song" /></div>;

  const s = song.data;

  return (
    <div className="container page">
      <Link to={`/albums/${albumId}`} className="back-link">
        <ArrowLeft size={18} aria-hidden="true" /> {album.data ? album.data.title : "Back to album"}
      </Link>

      <section className="song-hero">
        <p className="song-hero__track">Track {s.trackNumber}</p>
        <h1>{s.title}</h1>
        {album.data && (
          <p className="album-hero__artist">
            {album.data.artist}, from <Link to={`/albums/${albumId}`}>{album.data.title}</Link>
          </p>
        )}
        <ul className="facts">
          <li>
            <Clock size={18} aria-hidden="true" /> {formatDuration(s.durationSeconds)}
          </li>
          <li>
            <Rating value={s.averageRating} /> from {plural(s.reviewCount, "review")}
          </li>
        </ul>
      </section>

      <div className="song-columns">
        <section className="panel" aria-labelledby="your-review">
          <h2 id="your-review">Your review</h2>
          {!user && (
            <div className="empty">
              <p>Log in to rate this song.</p>
              <Link className="btn btn--primary" to="/login" state={{ from: `/albums/${albumId}/songs/${songId}` }}>
                <LogIn size={18} aria-hidden="true" /> Log in
              </Link>
            </div>
          )}
          {user && mine.loading && !mine.data && <Loading />}
          {user && mine.data && !myReview && <ReviewForm submitLabel="Post review" onSubmit={create} />}
          {user && myReview && !editing && (
            <ReviewCard
              review={myReview}
              actions={
                <>
                  <button className="btn btn--ghost btn--small" onClick={() => setEditing(true)}>
                    <Pencil size={16} aria-hidden="true" /> Edit
                  </button>
                  <button className="btn btn--danger btn--small" onClick={() => setToDelete(myReview)}>
                    <Trash2 size={16} aria-hidden="true" /> Delete
                  </button>
                </>
              }
            />
          )}
          {user && myReview && editing && (
            <ReviewForm initial={myReview} submitLabel="Save changes" onSubmit={update} onCancel={() => setEditing(false)} />
          )}
        </section>

        <section aria-labelledby="all-reviews">
          <div className="section-head">
            <h2 id="all-reviews">Reviews</h2>
            <div className="field field--inline">
              <label htmlFor="minRating">Show</label>
              <select id="minRating" value={minRating} onChange={(e) => setFilter(e.target.value)}>
                <option value="">All ratings</option>
                <option value="4">4 and up</option>
                <option value="3">3 and up</option>
                <option value="2">2 and up</option>
              </select>
            </div>
          </div>
          {reviews.error && <ErrorNotice error={reviews.error} onRetry={reviews.reload} />}
          {reviews.data && reviews.data.items.length === 0 && (
            <p className="muted">{minRating ? "No reviews at this rating yet." : "No reviews yet."}</p>
          )}
          <ul className="review-list">
            {reviews.data?.items.map((r) => (
              <li key={r.id}>
                <ReviewCard
                  review={r}
                  actions={
                    isAdmin && r.userId !== user?.id ? (
                      <button className="btn btn--danger btn--small" onClick={() => setToDelete(r)}>
                        <Trash2 size={16} aria-hidden="true" /> Remove
                      </button>
                    ) : undefined
                  }
                />
              </li>
            ))}
          </ul>
          {reviews.data && (
            <Pagination
              page={reviews.data.page}
              totalPages={reviews.data.totalPages}
              onChange={(p) => {
                const next = new URLSearchParams(params);
                next.set("page", String(p));
                setParams(next);
              }}
            />
          )}
        </section>
      </div>

      <Modal
        open={toDelete !== null}
        title={toDelete?.userId === user?.id ? "Delete your review?" : "Remove this review?"}
        onClose={() => {
          setToDelete(null);
          setDeleteError(null);
        }}
        footer={
          <>
            <button className="btn btn--ghost" onClick={() => setToDelete(null)}>
              Keep it
            </button>
            <button className="btn btn--danger" onClick={confirmDelete}>
              <Trash2 size={18} aria-hidden="true" /> Delete review
            </button>
          </>
        }
      >
        <p>
          {toDelete?.userId === user?.id
            ? "Your scores and comment for this song will be removed. You can write a new review afterwards."
            : `The review by ${toDelete?.username} will be removed for everyone.`}
        </p>
        {deleteError && <ErrorNotice error={deleteError} />}
      </Modal>
    </div>
  );
}
