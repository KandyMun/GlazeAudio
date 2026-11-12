import { useState, type FormEvent } from "react";
import { Loader2, Save } from "lucide-react";
import { api, ApiError } from "../api/client";
import type { Song } from "../api/types";
import { Field } from "./Field";
import { Modal } from "./Modal";

type SongFields = Pick<Song, "id" | "title" | "trackNumber" | "durationSeconds">;

interface Props {
  open: boolean;
  albumId: number;
  /** The song to edit, or null to add a new one. */
  song: SongFields | null;
  /** Suggested track number for a new song. */
  nextTrackNumber: number;
  onClose(): void;
  onSaved(song: Song): void;
}

/** Add / edit song form in a modal window (admins only). */
export function SongFormModal({ open, albumId, song, nextTrackNumber, onClose, onSaved }: Props) {
  return (
    <Modal open={open} title={song ? "Edit song" : "Add a song"} onClose={onClose} footer={
      <>
        <button className="btn btn--ghost" type="button" onClick={onClose}>
          Cancel
        </button>
        <button className="btn btn--primary" type="submit" form="song-form">
          <Save size={18} aria-hidden="true" /> {song ? "Save song" : "Add song"}
        </button>
      </>
    }>
      {open && <SongForm albumId={albumId} song={song} nextTrackNumber={nextTrackNumber} onSaved={onSaved} />}
    </Modal>
  );
}

function SongForm({ albumId, song, nextTrackNumber, onSaved }: Omit<Props, "open" | "onClose">) {
  const [title, setTitle] = useState(song?.title ?? "");
  const [trackNumber, setTrackNumber] = useState(String(song?.trackNumber ?? nextTrackNumber));
  const [minutes, setMinutes] = useState(song ? String(Math.floor(song.durationSeconds / 60)) : "");
  const [seconds, setSeconds] = useState(song ? String(song.durationSeconds % 60) : "");
  const [error, setError] = useState<ApiError | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    const duration = minutes || seconds ? Number(minutes || 0) * 60 + Number(seconds || 0) : null;
    const body = { title: title.trim(), trackNumber: trackNumber ? Number(trackNumber) : null, durationSeconds: duration };
    const base = `/api/albums/${albumId}/songs`;
    try {
      const saved = song
        ? await api<Song>(`${base}/${song.id}`, { method: "PUT", body })
        : await api<Song>(base, { method: "POST", body });
      onSaved(saved);
    } catch (err) {
      setError(err as ApiError);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form id="song-form" className="stack" onSubmit={submit} noValidate aria-busy={busy}>
      <Field id="song-title" label="Title" required maxLength={200} value={title} onChange={(e) => setTitle(e.target.value)} error={error?.fieldError("title")} />
      <div className="form-row">
        <Field id="song-track" label="Track number" type="number" required min={1} max={99} inputMode="numeric"
          value={trackNumber} onChange={(e) => setTrackNumber(e.target.value)} error={error?.fieldError("trackNumber")} />
        <fieldset className="duration">
          <legend>Length</legend>
          <div className="duration__inputs">
            <input id="song-minutes" type="number" min={0} max={120} inputMode="numeric" aria-label="Minutes" placeholder="3"
              value={minutes} onChange={(e) => setMinutes(e.target.value)} aria-invalid={!!error?.fieldError("durationSeconds")} />
            <span aria-hidden="true">:</span>
            <input id="song-seconds" type="number" min={0} max={59} inputMode="numeric" aria-label="Seconds" placeholder="45"
              value={seconds} onChange={(e) => setSeconds(e.target.value)} aria-invalid={!!error?.fieldError("durationSeconds")} />
          </div>
          <p className={`field__help${error?.fieldError("durationSeconds") ? " is-error" : ""}`}>
            {error?.fieldError("durationSeconds") ?? "Minutes and seconds."}
          </p>
        </fieldset>
      </div>
      {error && !error.problem.errors && <p className="form-error" role="alert">{error.problem.detail ?? error.problem.title}</p>}
      {busy && (
        <p className="loading">
          <Loader2 size={18} className="spin" aria-hidden="true" /> Saving…
        </p>
      )}
    </form>
  );
}
