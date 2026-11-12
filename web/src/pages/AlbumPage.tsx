import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Clock, ListMusic, MessageSquare, Pencil, Plus, Trash2 } from "lucide-react";
import { useAuth } from "../api/AuthContext";
import { api } from "../api/client";
import type { AlbumOverview } from "../api/types";
import { AlbumCover } from "../components/AlbumCover";
import { AlbumFormModal } from "../components/AlbumFormModal";
import { AspectBands } from "../components/AspectBands";
import { ConfirmModal } from "../components/ConfirmModal";
import { formatDate, formatDuration, formatLongDuration, plural } from "../components/format";
import { ErrorNotice, Loading } from "../components/Notice";
import { Rating } from "../components/Rating";
import { SongFormModal } from "../components/SongFormModal";
import { useToast } from "../components/Toast";
import { useApi } from "../components/useApi";

type OverviewSong = AlbumOverview["songs"][number];

export function AlbumPage() {
  const { albumId } = useParams();
  const { isAdmin } = useAuth();
  const navigate = useNavigate();
  const toast = useToast();
  const { data, error, loading, reload } = useApi<AlbumOverview>(`/api/albums/${albumId}/overview`);

  // Admin dialogs
  const [editingAlbum, setEditingAlbum] = useState(false);
  const [deletingAlbum, setDeletingAlbum] = useState(false);
  const [songForm, setSongForm] = useState<{ song: OverviewSong | null } | null>(null);
  const [songToDelete, setSongToDelete] = useState<OverviewSong | null>(null);

  if (loading && !data) return <div className="container page"><Loading label="Loading album" /></div>;
  if (error) return <div className="container page"><ErrorNotice error={error} onRetry={reload} /></div>;
  if (!data) return null;

  const { album, stats, songs, topRatedSong, latestReviews } = data;
  const nextTrackNumber = songs.reduce((max, s) => Math.max(max, s.trackNumber), 0) + 1;

  return (
    <div className="container page">
      <Link to="/" className="back-link">
        <ArrowLeft size={18} aria-hidden="true" /> All albums
      </Link>

      <section className="album-hero">
        <AlbumCover id={album.id} title={album.title} url={album.coverUrl} size="large" />
        <div className="album-hero__info">
          <p className="album-hero__genre">{album.genre}</p>
          <h1>{album.title}</h1>
          <p className="album-hero__artist">
            {album.artist}, {album.releaseYear}
          </p>
          <ul className="facts">
            <li>
              <ListMusic size={18} aria-hidden="true" /> {plural(stats.songCount, "song")}
            </li>
            <li>
              <Clock size={18} aria-hidden="true" /> {formatLongDuration(stats.totalDurationSeconds)}
            </li>
            <li>
              <MessageSquare size={18} aria-hidden="true" /> {plural(stats.reviewCount, "review")}
            </li>
          </ul>
          {isAdmin && (
            <div className="admin-bar" aria-label="Album admin actions">
              <button className="btn btn--ghost btn--small" onClick={() => setEditingAlbum(true)}>
                <Pencil size={16} aria-hidden="true" /> Edit album
              </button>
              <button className="btn btn--danger btn--small" onClick={() => setDeletingAlbum(true)}>
                <Trash2 size={16} aria-hidden="true" /> Delete album
              </button>
            </div>
          )}
        </div>
        <div className="album-hero__score">
          <p className="score-label">Overall</p>
          <Rating value={stats.averageRating} />
          {stats.aspectAverages ? (
            <AspectBands values={stats.aspectAverages} size="large" animate />
          ) : (
            <p className="muted">No reviews yet. Pick a song below and be the first.</p>
          )}
        </div>
      </section>

      <div className="album-columns">
        <section aria-labelledby="songs-title">
          <div className="section-head">
            <h2 id="songs-title">Songs</h2>
            {isAdmin && (
              <button className="btn btn--primary btn--small section-head__action" onClick={() => setSongForm({ song: null })}>
                <Plus size={16} aria-hidden="true" /> Add song
              </button>
            )}
          </div>
          {songs.length === 0 && (
            <div className="empty">
              <p>This album has no songs yet.</p>
              {isAdmin && (
                <button className="btn btn--primary" onClick={() => setSongForm({ song: null })}>
                  <Plus size={18} aria-hidden="true" /> Add the first song
                </button>
              )}
            </div>
          )}
          <ol className="track-list">
            {songs.map((song) => (
              <li key={song.id} className={isAdmin ? "track-row track-row--admin" : "track-row"}>
                <Link to={`/albums/${album.id}/songs/${song.id}`} className="track">
                  <span className="track__number">{song.trackNumber}</span>
                  <span className="track__title">
                    {song.title}
                    {topRatedSong?.id === song.id && <span className="tag tag--top">Top rated</span>}
                  </span>
                  <span className="track__duration muted">{formatDuration(song.durationSeconds)}</span>
                  <span className="track__rating">
                    <Rating value={song.averageRating} />
                    <span className="muted track__count">{plural(song.reviewCount, "review")}</span>
                  </span>
                </Link>
                {isAdmin && (
                  <span className="track__actions">
                    <button className="icon-btn" onClick={() => setSongForm({ song })} aria-label={`Edit ${song.title}`} title="Edit song">
                      <Pencil size={18} aria-hidden="true" />
                    </button>
                    <button className="icon-btn icon-btn--danger" onClick={() => setSongToDelete(song)} aria-label={`Delete ${song.title}`} title="Delete song">
                      <Trash2 size={18} aria-hidden="true" />
                    </button>
                  </span>
                )}
              </li>
            ))}
          </ol>
        </section>

        <section aria-labelledby="latest-title">
          <h2 id="latest-title">Latest reviews</h2>
          {latestReviews.length === 0 && <p className="muted">Nobody has reviewed a song from this album yet.</p>}
          <ul className="review-snippets">
            {latestReviews.map((r) => (
              <li key={r.id} className="review-snippet">
                <p className="review-snippet__head">
                  <Rating value={r.overallRating} label="Overall rating" />
                  <Link to={`/albums/${album.id}/songs/${r.songId}`}>{r.songTitle}</Link>
                </p>
                <blockquote>{r.comment}</blockquote>
                <p className="muted review-snippet__by">
                  <Link to={`/users/${r.userId}`}>{r.username}</Link>, {formatDate(r.createdAt)}
                </p>
              </li>
            ))}
          </ul>
        </section>
      </div>

      {isAdmin && (
        <>
          <AlbumFormModal
            open={editingAlbum}
            album={album}
            onClose={() => setEditingAlbum(false)}
            onSaved={(saved) => {
              setEditingAlbum(false);
              toast(`"${saved.title}" was saved.`);
              reload();
            }}
          />

          <ConfirmModal
            open={deletingAlbum}
            title="Delete this album?"
            confirmLabel="Delete album"
            cancelLabel="Keep it"
            typeToConfirm={album.title}
            onClose={() => setDeletingAlbum(false)}
            onConfirm={async () => {
              await api(`/api/albums/${album.id}`, { method: "DELETE" });
              setDeletingAlbum(false);
              toast(`"${album.title}" was deleted.`);
              navigate("/");
            }}
          >
            <p>
              <strong>{album.title}</strong> will be removed together with its {plural(stats.songCount, "song")} and{" "}
              {plural(stats.reviewCount, "review")}. This can't be undone.
            </p>
          </ConfirmModal>

          <SongFormModal
            open={songForm !== null}
            albumId={album.id}
            song={songForm?.song ?? null}
            nextTrackNumber={nextTrackNumber}
            onClose={() => setSongForm(null)}
            onSaved={(saved) => {
              toast(songForm?.song ? `"${saved.title}" was saved.` : `"${saved.title}" was added.`);
              setSongForm(null);
              reload();
            }}
          />

          <ConfirmModal
            open={songToDelete !== null}
            title="Delete this song?"
            confirmLabel="Delete song"
            cancelLabel="Keep it"
            onClose={() => setSongToDelete(null)}
            onConfirm={async () => {
              const song = songToDelete!;
              await api(`/api/albums/${album.id}/songs/${song.id}`, { method: "DELETE" });
              setSongToDelete(null);
              toast(`"${song.title}" was deleted.`);
              reload();
            }}
          >
            <p>
              <strong>{songToDelete?.title}</strong> and its {plural(songToDelete?.reviewCount ?? 0, "review")} will be removed.
              This can't be undone.
            </p>
          </ConfirmModal>
        </>
      )}
    </div>
  );
}
