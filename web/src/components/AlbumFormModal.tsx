import { useState, type FormEvent } from "react";
import { ImageOff, Link2, Loader2, Save } from "lucide-react";
import { api, ApiError } from "../api/client";
import type { Album } from "../api/types";
import { COVER_PRESETS } from "./covers";
import { Field } from "./Field";
import { Modal } from "./Modal";

const GENRE_SUGGESTIONS = ["Alternative Rock", "Rock", "Pop", "Hip-Hop", "Electronic", "Jazz", "Folk", "Soul", "R&B", "Metal", "Classical"];

type AlbumFields = Pick<Album, "id" | "title" | "artist" | "releaseYear" | "genre" | "coverUrl">;

interface Props {
  open: boolean;
  /** The album to edit, or null to add a new one. */
  album: AlbumFields | null;
  onClose(): void;
  onSaved(album: Album): void;
}

/** Add / edit album form in a modal window (admins only). */
export function AlbumFormModal({ open, album, onClose, onSaved }: Props) {
  return (
    <Modal open={open} title={album ? "Edit album" : "Add an album"} onClose={onClose} wide footer={
      <>
        <button className="btn btn--ghost" type="button" onClick={onClose}>
          Cancel
        </button>
        <button className="btn btn--primary" type="submit" form="album-form">
          <Save size={18} aria-hidden="true" /> {album ? "Save album" : "Add album"}
        </button>
      </>
    }>
      {open && <AlbumForm album={album} onSaved={onSaved} />}
    </Modal>
  );
}

type CoverChoice = "none" | "custom" | string;

function AlbumForm({ album, onSaved }: { album: AlbumFields | null; onSaved(album: Album): void }) {
  const initialChoice: CoverChoice = !album?.coverUrl
    ? "none"
    : COVER_PRESETS.some((c) => c.url === album.coverUrl)
      ? album.coverUrl
      : "custom";

  const [form, setForm] = useState({
    title: album?.title ?? "",
    artist: album?.artist ?? "",
    releaseYear: album ? String(album.releaseYear) : "",
    genre: album?.genre ?? "",
  });
  const [cover, setCover] = useState<CoverChoice>(initialChoice);
  const [customUrl, setCustomUrl] = useState(initialChoice === "custom" ? album!.coverUrl! : "");
  const [error, setError] = useState<ApiError | null>(null);
  const [busy, setBusy] = useState(false);

  const set = (key: keyof typeof form) => (e: { target: { value: string } }) => setForm({ ...form, [key]: e.target.value });

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    const body = {
      title: form.title.trim(),
      artist: form.artist.trim(),
      releaseYear: form.releaseYear ? Number(form.releaseYear) : null,
      genre: form.genre.trim(),
      coverUrl: cover === "none" ? null : cover === "custom" ? customUrl.trim() || null : cover,
    };
    try {
      const saved = album
        ? await api<Album>(`/api/albums/${album.id}`, { method: "PUT", body })
        : await api<Album>("/api/albums", { method: "POST", body });
      onSaved(saved);
    } catch (err) {
      setError(err as ApiError);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form id="album-form" className="album-form" onSubmit={submit} noValidate aria-busy={busy}>
      <div className="album-form__fields">
        <Field id="album-title" label="Title" required maxLength={200} value={form.title} onChange={set("title")} error={error?.fieldError("title")} />
        <Field id="album-artist" label="Artist" required maxLength={200} value={form.artist} onChange={set("artist")} error={error?.fieldError("artist")} />
        <div className="form-row">
          <Field id="album-year" label="Release year" type="number" required min={1900} max={2100} inputMode="numeric"
            value={form.releaseYear} onChange={set("releaseYear")} error={error?.fieldError("releaseYear")} />
          <Field id="album-genre" label="Genre" required maxLength={100} list="genre-suggestions"
            value={form.genre} onChange={set("genre")} error={error?.fieldError("genre")} />
          <datalist id="genre-suggestions">
            {GENRE_SUGGESTIONS.map((g) => (
              <option key={g} value={g} />
            ))}
          </datalist>
        </div>
      </div>

      <fieldset className="cover-picker">
        <legend>Cover</legend>
        <div className="cover-picker__grid">
          {COVER_PRESETS.map((c) => (
            <label key={c.url} className="cover-option">
              <input type="radio" name="cover" value={c.url} checked={cover === c.url} onChange={() => setCover(c.url)} />
              <img src={c.url} alt="" width={96} height={96} loading="lazy" />
              <span className="cover-option__name">{c.name}</span>
            </label>
          ))}
          <label className="cover-option">
            <input type="radio" name="cover" value="none" checked={cover === "none"} onChange={() => setCover("none")} />
            <span className="cover-option__icon"><ImageOff size={28} aria-hidden="true" /></span>
            <span className="cover-option__name">No image</span>
          </label>
          <label className="cover-option">
            <input type="radio" name="cover" value="custom" checked={cover === "custom"} onChange={() => setCover("custom")} />
            <span className="cover-option__icon"><Link2 size={28} aria-hidden="true" /></span>
            <span className="cover-option__name">From a link</span>
          </label>
        </div>
        {cover === "custom" && (
          <Field id="album-cover-url" label="Image address" type="url" placeholder="https://…" maxLength={500}
            value={customUrl} onChange={(e) => setCustomUrl(e.target.value)} error={error?.fieldError("coverUrl")}
            help="Paste a link to a JPG, PNG or WebP image." />
        )}
        {cover !== "custom" && error?.fieldError("coverUrl") && <p className="form-error">{error.fieldError("coverUrl")}</p>}
      </fieldset>

      {error && !error.problem.errors && <p className="form-error" role="alert">{error.problem.detail ?? error.problem.title}</p>}
      {busy && (
        <p className="loading">
          <Loader2 size={18} className="spin" aria-hidden="true" /> Saving…
        </p>
      )}
    </form>
  );
}
